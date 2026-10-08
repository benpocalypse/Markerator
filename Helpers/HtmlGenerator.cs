using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using FluentResults;
using Markdig;
using Markerator.Abstractions;

namespace Markerator.Helpers
{
    /// <summary>
    /// Orchestrates generation of a Markerator site: reads markdown from the input
    /// directory, renders it to HTML, copies asset folders, and writes the resulting
    /// site tree to the output directory.
    /// </summary>
    public class HtmlGenerator
    {
        private readonly string _siteTitle;
        private readonly string _baseUrl;
        private readonly string _inputDir;
        private readonly string _outputDir;
        private readonly string _css;
        private readonly bool _favicon;
        private readonly bool _rssFeed;
        private readonly bool _rssIcon;
        private readonly int _postsPerPage;
        private readonly IReadOnlyList<string> _postsTitles;
        private readonly IReadOnlyList<string> _otherPages;

        private readonly MarkdownPipeline _markdownPipeline;
        private readonly string _defaultOgImageUrl;

        /// <summary>
        /// Initializes a new <see cref="HtmlGenerator"/> with the paths and options
        /// needed for a single site generation run.
        /// </summary>
        /// <param name="title">The site title, used in nav, <c>&lt;title&gt;</c>, and OG metadata.</param>
        /// <param name="baseUrl">The base URL, with any trailing slash stripped.</param>
        /// <param name="inputDir">The absolute path to the input directory.</param>
        /// <param name="outputDir">The absolute path to the output directory.</param>
        /// <param name="css">Optional custom CSS file name; if empty, a built-in default is used.</param>
        /// <param name="favicon">Whether to emit a favicon <c>&lt;link&gt;</c>.</param>
        /// <param name="rssFeed">Whether to generate RSS feeds for each post section.</param>
        /// <param name="rssIcon">Whether to render an RSS icon on section pages.</param>
        /// <param name="postsPerPage">The number of posts per section page; 0 disables pagination.</param>
        /// <param name="postsTitles">The names of the post sections (e.g. News, Blog).</param>
        /// <param name="otherPages">Additional markdown page file names to render.</param>
        public HtmlGenerator(
            string title,
            string baseUrl,
            string inputDir,
            string outputDir,
            string css,
            bool favicon,
            bool rssFeed,
            bool rssIcon,
            int postsPerPage,
            IReadOnlyList<string> postsTitles,
            IReadOnlyList<string> otherPages)
        {
            _siteTitle = title;
            _baseUrl = baseUrl.TrimEnd('/');
            _inputDir = inputDir;
            _outputDir = outputDir;
            _css = css;
            _favicon = favicon;
            _rssFeed = rssFeed;
            _rssIcon = rssIcon;
            _postsPerPage = postsPerPage;
            _postsTitles = postsTitles ?? new List<string>();
            _otherPages = otherPages ?? new List<string>();

            _markdownPipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

            _defaultOgImageUrl = ResolveDefaultOgImageUrl();
        }

        /// <summary>
        /// Runs the full generation pipeline: writes the stylesheet, copies asset
        /// folders, builds the shared navigation, and generates the index, post
        /// section, and extra pages. Returns a failed <see cref="Result"/> if any
        /// step throws or if the index file is missing.
        /// </summary>
        /// <param name="indexFile">The markdown file to render as index.html.</param>
        /// <returns>A <see cref="Result"/> indicating success or carrying error messages.</returns>
        public Result Generate(string indexFile)
        {
            try
            {
                // 0. Write the stylesheet first, so every page's <link> resolves.
                WriteCssFile();

                // 0b. Copy asset folders (images/, fonts/, etc.) to output/.
                CopyAssetFolders();

                // 1. Build the shared nav.
                var navHtml = BuildNavigation();

                // 2. Generate index.html
                var indexResult = GenerateIndex(indexFile, navHtml);
                if (indexResult.IsFailed) return indexResult;

                // 3. Generate each post section.
                foreach (var section in _postsTitles)
                {
                    var sectionResult = GeneratePostsSection(section, navHtml);
                    if (sectionResult.IsFailed) return sectionResult;
                }

                // 4. Generate extra standalone pages.
                foreach (var page in _otherPages)
                {
                    var pageResult = GenerateSinglePage(page, navHtml);
                    if (pageResult.IsFailed) return pageResult;
                }

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(new Error($"Unhandled exception during generation: {ex.Message}"));
            }
        }

        /// <summary>
        /// Resolves and writes the site stylesheet to <c>output/css/</c>. Prefers an
        /// explicitly requested CSS file, falls back to <c>input/css/site.css</c>,
        /// and finally writes <see cref="Globals.DefaultCss"/> so the stylesheet link
        /// always resolves.
        /// </summary>
        private void WriteCssFile()
        {
            var cssOutDir = Path.Combine(_outputDir, "css");
            Directory.CreateDirectory(cssOutDir);

            // 1. Explicitly requested CSS: find it and copy verbatim.
            if (!string.IsNullOrEmpty(_css))
            {
                var candidates = new[]
                {
                    Path.Combine(_inputDir, _css),
                    Path.Combine(_inputDir, "css", _css),
                    Path.Combine(_inputDir, "css", Path.GetFileName(_css))
                };

                var source = candidates.FirstOrDefault(File.Exists);
                if (source != null)
                {
                    var destName = Path.GetFileName(source);
                    var dest = Path.Combine(cssOutDir, destName);
                    File.Copy(source, dest, overwrite: true);
                    Console.WriteLine($"Copied {source} -> {dest}");
                    return;
                }

                Console.WriteLine(
                    $"Warning: CSS file '{_css}' was specified but could not be located in " +
                    $"input/ or input/css/. Falling back to the default stylesheet.");
            }

            // 2. No CSS specified (or the requested one wasn't found): try input/css/site.css.
            var siteCssSource = Path.Combine(_inputDir, "css", "site.css");
            if (File.Exists(siteCssSource))
            {
                var dest = Path.Combine(cssOutDir, "site.css");
                File.Copy(siteCssSource, dest, overwrite: true);
                Console.WriteLine($"Copied {siteCssSource} -> {dest}");
                return;
            }

            // 3. Nothing user-provided: write Globals.DefaultCss so the <link> always resolves.
            var defaultDest = Path.Combine(cssOutDir, "site.css");
            File.WriteAllText(defaultDest, Globals.DefaultCss);
            Console.WriteLine($"Wrote built-in default stylesheet -> {defaultDest}");
        }

        /// <summary>
        /// Copies every folder under <c>input/</c> that does not contain markdown files
        /// (recursively) to the corresponding location under <c>output/</c>. Folders
        /// that contain markdown are treated as post sections and handled by
        /// <see cref="GeneratePostsSection"/>; the <c>css/</c> and <c>Themes/</c>
        /// folders are excluded because <see cref="WriteCssFile"/> already handles them.
        /// Nested directory structure is preserved.
        /// </summary>
        private void CopyAssetFolders()
{
    // Folders whose contents are already handled by WriteCssFile.
    var excludedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "css",
        "Themes"
    };

    var topLevelDirs = Directory.GetDirectories(_inputDir, "*", SearchOption.TopDirectoryOnly);
    if (topLevelDirs.Length == 0)
    {
        Console.WriteLine("No input subfolders found; skipping asset copy.");
        return;
    }

    Console.WriteLine("------------------------------------------------------------");
    Console.WriteLine("Copying asset folders");
    Console.WriteLine($"  Source:      {_inputDir}");
    Console.WriteLine($"  Destination: {_outputDir}");

    int foldersCopied = 0;
    int filesCopied = 0;

    foreach (var sourceDir in topLevelDirs)
    {
        var folderName = Path.GetFileName(sourceDir);

        // Skip hidden/dot directories like .git, .github, .vscode.
        // These are tooling metadata and never belong in the generated site.
        if (folderName.StartsWith("."))
        {
            Console.WriteLine($"    Skipped: {folderName}/ (hidden directory)");
            continue;
        }

        if (excludedFolders.Contains(folderName))
        {
            Console.WriteLine($"    Skipped: {folderName}/ (handled by WriteCssFile)");
            continue;
        }

        var markdownFiles = Directory.GetFiles(sourceDir, "*.md", SearchOption.AllDirectories);
        if (markdownFiles.Length > 0)
        {
            Console.WriteLine($"    Skipped: {folderName}/ (contains {markdownFiles.Length} markdown file(s))");
            continue;
        }

        var destDir = Path.Combine(_outputDir, folderName);
        Directory.CreateDirectory(destDir);

        var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
        foreach (var sourceFile in files)
        {
            // Also skip anything inside a hidden subdirectory (e.g. a nested
            // .git inside a copied folder).
            var relative = Path.GetRelativePath(sourceDir, sourceFile);
            if (relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Any(segment => segment.StartsWith(".")))
            {
                continue;
            }

            var destFile = Path.Combine(destDir, relative);

            var destFileDir = Path.GetDirectoryName(destFile);
            if (!string.IsNullOrEmpty(destFileDir))
            {
                Directory.CreateDirectory(destFileDir);
            }

            File.Copy(sourceFile, destFile, overwrite: true);
            filesCopied++;
        }

        Console.WriteLine($"    Copied: {folderName}/ ({files.Length} file(s))");
        foldersCopied++;
    }

    Console.WriteLine($"  Copied {foldersCopied} folder(s) / {filesCopied} file(s).");
    Console.WriteLine("------------------------------------------------------------");
}

        /// <summary>
        /// Builds the navigation HTML from the configured post sections and extra pages.
        /// Each section links to its canonical first page (e.g. <c>/News.html</c>),
        /// and each extra page links to <c>/{name}.html</c>.
        /// </summary>
        /// <returns>The rendered navigation markup.</returns>
        private string BuildNavigation()
        {
            var sb = new StringBuilder();
            sb.AppendLine("    <div class=\"navigation\">");

            foreach (var section in _postsTitles)
            {
                sb.AppendLine($"      <a href=\"/{section}.html\">{section}</a>");
            }

            foreach (var page in _otherPages)
            {
                var name = Path.GetFileNameWithoutExtension(page);
                sb.AppendLine($"      <a href=\"/{name}.html\">{name}</a>");
            }

            sb.AppendLine("    </div>");
            return sb.ToString();
        }

        /// <summary>
        /// Reads the index markdown file, renders it to HTML, and writes
        /// <c>index.html</c> with the appropriate Open Graph metadata.
        /// </summary>
        /// <param name="indexFile">The markdown file name, relative to the input directory.</param>
        /// <param name="navHtml">The shared navigation markup.</param>
        /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
        private Result GenerateIndex(string indexFile, string navHtml)
        {
            var indexPath = Path.Combine(_inputDir, indexFile);
            if (!File.Exists(indexPath))
            {
                return Result.Fail(new Error($"Index file not found: {indexPath}"));
            }

            var rawMarkdown = File.ReadAllText(indexPath);
            var bodyHtml = Markdown.ToHtml(rawMarkdown, _markdownPipeline);

            var description = OpenGraphHelper.GenerateDescription(rawMarkdown, 160);

            var ogData = new OpenGraphData
            {
                Title = _siteTitle,
                Description = description,
                ImageUrl = _defaultOgImageUrl,
                Url = _baseUrl + "/",
                Type = "website",
                SiteName = _siteTitle
            };

            var fullHtml = BuildFullHtmlDocument(
                title: _siteTitle,
                navHtml: navHtml,
                bodyHtml: bodyHtml,
                ogData: ogData,
                extraHeadTags: string.Empty);

            var outPath = Path.Combine(_outputDir, "index.html");
            File.WriteAllText(outPath, fullHtml);
            Console.WriteLine($"Generated {outPath}");

            return Result.Ok();
        }

        /// <summary>
        /// Generates all pages for a single post section: the paginated section
        /// listing(s), the individual post pages, and (optionally) the RSS feed.
        /// Posts are sorted newest-first with undated posts last, and grouped by
        /// year in the listing output.
        /// </summary>
        /// <param name="sectionName">The section name, matching a folder under <c>input/</c>.</param>
        /// <param name="navHtml">The shared navigation markup.</param>
        /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
        private Result GeneratePostsSection(string sectionName, string navHtml)
        {
            var sectionInput = Path.Combine(_inputDir, sectionName);
            if (!Directory.Exists(sectionInput))
            {
                Console.WriteLine($"Warning: no folder for section '{sectionName}' at {sectionInput}. Skipping.");
                return Result.Ok();
            }

            // 1. Load and parse all posts.
            var postFiles = Directory.GetFiles(sectionInput, "*.md", SearchOption.TopDirectoryOnly);
            var posts = new List<PostEntry>();

            foreach (var file in postFiles)
            {
                var raw = File.ReadAllText(file);
                var (date, parsedTitle, cleanMarkdown) = ExtractAndStripDateHeader(raw);
                var htmlContent = Markdown.ToHtml(cleanMarkdown, _markdownPipeline);
                var fileName = Path.GetFileNameWithoutExtension(file);

                var derivedTitle = string.IsNullOrWhiteSpace(parsedTitle) ? fileName : parsedTitle;

                posts.Add(new PostEntry
                {
                    SourceFile = file,
                    FileName = fileName,
                    Title = derivedTitle,
                    Date = date,
                    RawMarkdown = raw,
                    HtmlContent = htmlContent,
                    Summary = OpenGraphHelper.GenerateDescription(cleanMarkdown, 200)
                });
            }

            // 2. Sort: dated first (newest first), then undated.
            var sorted = posts
                .OrderByDescending(p => p.Date.HasValue)
                .ThenByDescending(p => p.Date ?? DateTime.MinValue)
                .ThenBy(p => p.FileName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // 3. Paginate.
            var pages = PaginationHelper.Paginate(sorted, _postsPerPage);
            var totalPages = pages.Count;

            // 4. Write each section listing page.
            for (int i = 0; i < totalPages; i++)
            {
                var pageNumber = i + 1;
                var fileName = PaginationHelper.GetPageFileName(sectionName, pageNumber);
                var outPath = Path.Combine(_outputDir, fileName);

                var bodyBuilder = new StringBuilder();
                bodyBuilder.AppendLine($"<h2>{sectionName}</h2>");

                if (_rssIcon && _rssFeed)
                {
                    var iconUrl = ResolveRssIconUrl();
                    if (!string.IsNullOrEmpty(iconUrl))
                    {
                        bodyBuilder.AppendLine(
                            $"<a class=\"rss-link\" href=\"/{sectionName}.xml\">" +
                            $"<img src=\"{iconUrl}\" alt=\"RSS Feed\" /></a>");
                    }
                }

                if (pages[i].Count == 0)
                {
                    bodyBuilder.AppendLine("<p>No posts yet.</p>");
                }
                else
                {
                    var groups = pages[i]
                        .GroupBy(p => p.Date.HasValue
                            ? p.Date.Value.Year.ToString()
                            : "All");

                    var orderedGroups = groups
                        .OrderByDescending(g => g.Key == "All"
                            ? int.MinValue
                            : int.Parse(g.Key));

                    foreach (var group in orderedGroups)
                    {
                        bodyBuilder.AppendLine($"<h3>{group.Key}</h3>");

                        foreach (var post in group)
                        {
                            var link = $"/{sectionName}/{post.FileName}.html";
                            var titleText = WebUtility.HtmlEncode(post.Title);

                            if (post.Date.HasValue)
                            {
                                var dateStr = post.Date.Value.ToString("MM/dd");
                                bodyBuilder.AppendLine($"<a href=\"{link}\">{dateStr} - {titleText}</a><br/>");
                            }
                            else
                            {
                                bodyBuilder.AppendLine($"<a href=\"{link}\">{titleText}</a><br/>");
                            }

                            bodyBuilder.AppendLine(WebUtility.HtmlEncode(post.Summary));
                            bodyBuilder.AppendLine("<br/>");
                            bodyBuilder.AppendLine("<br/>");
                        }
                    }
                }

                bodyBuilder.AppendLine(PaginationHelper.GeneratePaginationLinks(
                    sectionName, pageNumber, totalPages, _baseUrl));

                var bodyHtml = bodyBuilder.ToString();

                var pageTitle = pageNumber > 1
                    ? $"{_siteTitle} - {sectionName} (Page {pageNumber})"
                    : $"{_siteTitle} - {sectionName}";

                var ogData = new OpenGraphData
                {
                    Title = pageTitle,
                    Description = $"Browse {sectionName} posts on {_siteTitle}.",
                    ImageUrl = _defaultOgImageUrl,
                    Url = $"{_baseUrl}/{fileName}",
                    Type = "website",
                    SiteName = _siteTitle
                };

                var relLinks = PaginationHelper.GenerateRelLinks(
                    sectionName, pageNumber, totalPages, _baseUrl);

                var fullHtml = BuildFullHtmlDocument(
                    title: pageTitle,
                    navHtml: navHtml,
                    bodyHtml: bodyHtml,
                    ogData: ogData,
                    extraHeadTags: relLinks);

                File.WriteAllText(outPath, fullHtml);
                Console.WriteLine($"Generated {outPath}");
            }

            // 5. Write individual post pages.
            var postOutputDir = Path.Combine(_outputDir, sectionName);
            Directory.CreateDirectory(postOutputDir);

            foreach (var post in sorted)
            {
                var postPath = Path.Combine(postOutputDir, $"{post.FileName}.html");
                var postTitle = $"{_siteTitle} - {post.Title}";

                var ogData = new OpenGraphData
                {
                    Title = post.Title,
                    Description = post.Summary,
                    ImageUrl = _defaultOgImageUrl,
                    Url = $"{_baseUrl}/{sectionName}/{post.FileName}.html",
                    Type = "article",
                    SiteName = _siteTitle
                };

                var fullHtml = BuildFullHtmlDocument(
                    title: postTitle,
                    navHtml: navHtml,
                    bodyHtml: post.HtmlContent,
                    ogData: ogData,
                    extraHeadTags: string.Empty);

                File.WriteAllText(postPath, fullHtml);
                Console.WriteLine($"Generated {postPath}");
            }

            // 6. RSS feed.
            if (_rssFeed)
            {
                WriteRssFeed(sectionName, sorted);
            }

            return Result.Ok();
        }

        /// <summary>
        /// Renders a single markdown file that is not part of a post section (for
        /// example, an About or Contact page) and writes it as <c>output/{name}.html</c>.
        /// </summary>
        /// <param name="mdFileName">The markdown file name, relative to the input directory.</param>
        /// <param name="navHtml">The shared navigation markup.</param>
        /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
        private Result GenerateSinglePage(string mdFileName, string navHtml)
        {
            var path = Path.Combine(_inputDir, mdFileName);
            if (!File.Exists(path))
            {
                return Result.Fail(new Error($"Extra page not found: {path}"));
            }

            var rawMarkdown = File.ReadAllText(path);
            var bodyHtml = Markdown.ToHtml(rawMarkdown, _markdownPipeline);
            var name = Path.GetFileNameWithoutExtension(mdFileName);

            var ogData = new OpenGraphData
            {
                Title = $"{_siteTitle} - {name}",
                Description = OpenGraphHelper.GenerateDescription(rawMarkdown, 160),
                ImageUrl = _defaultOgImageUrl,
                Url = $"{_baseUrl}/{name}.html",
                Type = "website",
                SiteName = _siteTitle
            };

            var fullHtml = BuildFullHtmlDocument(
                title: $"{_siteTitle} - {name}",
                navHtml: navHtml,
                bodyHtml: bodyHtml,
                ogData: ogData,
                extraHeadTags: string.Empty);

            var outPath = Path.Combine(_outputDir, $"{name}.html");
            File.WriteAllText(outPath, fullHtml);
            Console.WriteLine($"Generated {outPath}");

            return Result.Ok();
        }

        /// <summary>
        /// Assembles a complete HTML document from the given title, navigation,
        /// body content, Open Graph metadata, and any additional head tags.
        /// </summary>
        /// <param name="title">The page title, rendered in <c>&lt;title&gt;</c>.</param>
        /// <param name="navHtml">The shared navigation markup.</param>
        /// <param name="bodyHtml">The main content markup for the page.</param>
        /// <param name="ogData">The Open Graph metadata to render in the head.</param>
        /// <param name="extraHeadTags">Additional markup to inject in the head (e.g. rel links).</param>
        /// <returns>The complete HTML document as a string.</returns>
        private string BuildFullHtmlDocument(
        string title,
        string navHtml,
        string bodyHtml,
        OpenGraphData ogData,
        string extraHeadTags)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"utf-8\" />");
            sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
            sb.AppendLine($"  <title>{System.Net.WebUtility.HtmlEncode(title)}</title>");

            var cssHref = string.IsNullOrEmpty(_css)
                ? "/css/site.css"
                : $"/css/{Path.GetFileName(_css)}";
            sb.AppendLine($"  <link rel=\"stylesheet\" href=\"{cssHref}\" />");

            if (_favicon)
            {
                sb.AppendLine("  <link rel=\"icon\" href=\"/images/favicon.ico\" />");
            }

            if (ogData != null!)
            {
                sb.AppendLine(ogData.GenerateMetaTags());
            }

            if (!string.IsNullOrEmpty(extraHeadTags))
            {
                sb.AppendLine(extraHeadTags);
            }

            sb.AppendLine("</head>");
            sb.AppendLine("<body id=\"theme_default\">");

            // The radio group controls the theme. Both radios share the name "mode", so
            // only one can be checked at a time. The "checked" attribute on the light
            // radio is the JS-free default; CSS media queries and user interaction can
            // override it visually. Because these radios are siblings that precede the
            // .color-scheme-wrapper, :checked ~ .color-scheme-wrapper can reach it.
            sb.AppendLine("  <input type=\"radio\" name=\"mode\" id=\"mode_light\" value=\"light\" class=\"mode-input\" checked=\"checked\" />");
            sb.AppendLine("  <input type=\"radio\" name=\"mode\" id=\"mode_dark\" value=\"dark\" class=\"mode-input\" />");

            sb.AppendLine("  <div class=\"color-scheme-wrapper\">");
            sb.AppendLine("    <div class=\"navigation-title\">");
            sb.AppendLine($"      <a href=\"/\">{System.Net.WebUtility.HtmlEncode(_siteTitle)}</a>");
            sb.AppendLine("      <div class=\"mode-toggle\">");
            sb.AppendLine("        <label for=\"mode_light\" class=\"mode-label\">light</label>");
            sb.AppendLine("        <label for=\"mode_dark\" class=\"mode-label\">dark</label>");
            sb.AppendLine("      </div>");
            sb.AppendLine("    </div>");
            sb.AppendLine(navHtml);
            sb.AppendLine("    <div class=\"content\">");
            sb.AppendLine(bodyHtml);
            sb.AppendLine("    </div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        /// <summary>
        /// Writes an RSS 2.0 feed for a post section to <c>output/{sectionName}.xml</c>.
        /// Contains up to the 50 most recent posts, in the order they are supplied.
        /// </summary>
        /// <param name="sectionName">The section name, used for the feed title and output file.</param>
        /// <param name="posts">The posts to include, in display order.</param>
        private void WriteRssFeed(string sectionName, List<PostEntry> posts)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<rss version=\"2.0\">");
            sb.AppendLine("  <channel>");
            sb.AppendLine($"    <title>{WebUtility.HtmlEncode(_siteTitle)} - {sectionName}</title>");
            sb.AppendLine($"    <link>{_baseUrl}/{sectionName}.html</link>");
            sb.AppendLine($"    <description>Latest {sectionName} posts</description>");

            foreach (var post in posts.Take(50))
            {
                var link = $"{_baseUrl}/{sectionName}/{post.FileName}.html";
                var pubDate = (post.Date ?? DateTime.UtcNow).ToString("R");

                sb.AppendLine("    <item>");
                sb.AppendLine($"      <title>{WebUtility.HtmlEncode(post.Title)}</title>");
                sb.AppendLine($"      <link>{link}</link>");
                sb.AppendLine($"      <guid>{link}</guid>");
                sb.AppendLine($"      <pubDate>{pubDate}</pubDate>");
                sb.AppendLine($"      <description>{WebUtility.HtmlEncode(post.Summary)}</description>");
                sb.AppendLine("    </item>");
            }

            sb.AppendLine("  </channel>");
            sb.AppendLine("</rss>");

            var outPath = Path.Combine(_outputDir, $"{sectionName}.xml");
            File.WriteAllText(outPath, sb.ToString());
            Console.WriteLine($"Generated {outPath}");
        }

        /// <summary>
        /// Extracts a leading <c># YYYY-MM-DD Title</c> header from the markdown, if
        /// present, returning the parsed date, the title, and the markdown with the
        /// header line removed. If no dated header is found, the title is derived
        /// from the first heading, and the markdown is returned unchanged.
        /// </summary>
        /// <param name="markdown">The raw markdown content of a post.</param>
        /// <returns>
        /// A tuple containing the parsed date (or null), the extracted title (or null),
        /// and the cleaned markdown.
        /// </returns>
        private static (DateTime? date, string title, string cleanMarkdown)
            ExtractAndStripDateHeader(string markdown)
        {
            var match = Regex.Match(
                markdown,
                @"^#\s+(\d{4}-\d{2}-\d{2})\s*(.*)$",
                RegexOptions.Multiline);

            if (match.Success && DateTime.TryParse(match.Groups[1].Value, out var parsedDate))
            {
                var title = match.Groups[2].Value.Trim();
                var cleaned = markdown.Remove(match.Index, match.Length).TrimStart();
                return (parsedDate, title, cleaned);
            }

            // No date header: try to derive a title from an H1 if present.
            var h1 = Regex.Match(markdown, @"^#\s+(.+)$", RegexOptions.Multiline);
            var fallbackTitle = h1.Success ? h1.Groups[1].Value.Trim() : null;

            return (null, fallbackTitle, markdown)!;
        }

        /// <summary>
        /// Resolves the absolute URL of the site's default Open Graph image by
        /// checking <c>input/images/</c> for a well-known filename. Falls back to
        /// <c>cardimage.png</c> if none of the candidates exist.
        /// </summary>
        /// <returns>The absolute URL to use for Open Graph image metadata.</returns>
        private string ResolveDefaultOgImageUrl()
        {
            var candidates = new[]
            {
                "cardimage.png",
                "cardimage.jpg",
                "og-image.jpg",
                "og-image.png",
                "social.jpg",
                "social.png"
            };

            foreach (var name in candidates)
            {
                var path = Path.Combine(_inputDir, "images", name);
                if (File.Exists(path))
                {
                    return $"{_baseUrl}/images/{name}";
                }
            }

            return $"{_baseUrl}/images/og-image.png";
        }

        /// <summary>
        /// Resolves the site-relative URL of an RSS icon in <c>input/images/</c>,
        /// preferring <c>rss.jpg</c> over <c>rss.png</c>.
        /// </summary>
        /// <returns>The relative URL of the icon, or <c>null</c> if neither file exists.</returns>
        private string ResolveRssIconUrl()
        {
            if (File.Exists(Path.Combine(_inputDir, "images", "rss.jpg")))
                return "/images/rss.jpg";
            
            if (File.Exists(Path.Combine(_inputDir, "images", "rss.png")))
                return "/images/rss.png";
            
            return string.Empty;
        }
    }
}
