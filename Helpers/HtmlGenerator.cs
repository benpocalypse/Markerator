using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using FluentResults;
using Markdig;
using Markerator.Abstractions;

namespace Markerator.Helpers
{
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

        // --------------------------------------------------------------------
        // Public entry point
        // --------------------------------------------------------------------

        public Result Generate(string indexFile)
        {
            try
            {
                // 0. Write the stylesheet first, so every page's <link> resolves.
                WriteCssFile();

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

        // --------------------------------------------------------------------
        // CSS handling
        // --------------------------------------------------------------------

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

        // --------------------------------------------------------------------
        // Navigation
        // --------------------------------------------------------------------

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

        // --------------------------------------------------------------------
        // Index page
        // --------------------------------------------------------------------

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

        // --------------------------------------------------------------------
        // Posts sections
        // --------------------------------------------------------------------

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

        // --------------------------------------------------------------------
        // Extra standalone pages
        // --------------------------------------------------------------------

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

        // --------------------------------------------------------------------
        // HTML template
        // --------------------------------------------------------------------

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
            sb.AppendLine($"  <title>{WebUtility.HtmlEncode(title)}</title>");

            var cssHref = string.IsNullOrEmpty(_css)
                ? "/css/site.css"
                : $"/css/{Path.GetFileName(_css)}";
            sb.AppendLine($"  <link rel=\"stylesheet\" href=\"{cssHref}\" />");

            if (_favicon)
            {
                sb.AppendLine("  <link rel=\"icon\" href=\"/images/favicon.ico\" />");
            }

            if (ogData != null)
            {
                sb.AppendLine(ogData.GenerateMetaTags());
            }

            if (!string.IsNullOrEmpty(extraHeadTags))
            {
                sb.AppendLine(extraHeadTags);
            }

            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("  <div class=\"navigation-title\">");
            sb.AppendLine($"    <a href=\"/\">{WebUtility.HtmlEncode(_siteTitle)}</a>");
            sb.AppendLine("  </div>");
            sb.AppendLine(navHtml);
            sb.AppendLine("  <div class=\"content\">");
            sb.AppendLine(bodyHtml);
            sb.AppendLine("  </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        // --------------------------------------------------------------------
        // RSS
        // --------------------------------------------------------------------

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

        // --------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------

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

            return (null, fallbackTitle, markdown);
        }

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

            return $"{_baseUrl}/images/cardimage.png";
        }

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