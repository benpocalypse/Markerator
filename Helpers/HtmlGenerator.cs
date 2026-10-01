using System.Text;
using System.Text.RegularExpressions;
using FluentResults;
using Markdig;
using Markerator.Abstractions;

namespace Markerator.Helpers;

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
    private readonly List<string> _postsTitles;
    private readonly List<string> _otherPages;

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
        List<string> postsTitles,
        List<string> otherPages)
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
    

    public Result Generate(string indexFile)
    {
        try
        {
            // 1. Build the shared nav (post sections + extra pages)
            var navHtml = BuildNavigation();

            // 2. Generate index.html
            var indexResult = GenerateIndex(indexFile, navHtml);
            if (indexResult.IsFailed) return indexResult;

            // 3. Generate each post section (with pagination + RSS)
            if (_postsTitles.Any())
            {
                foreach (var section in _postsTitles)
                {
                    var sectionResult = GeneratePostsSection(section, navHtml);
                    if (sectionResult.IsFailed) return sectionResult;
                }
            }

            // 4. Generate any extra standalone pages
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
    

    private string BuildNavigation()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<nav class=\"site-nav\"><ul>");

        // Post sections link to their canonical first page (e.g. /News.html)
        foreach (var section in _postsTitles)
        {
            sb.AppendLine($"  <li><a href=\"/{section}.html\">{section}</a></li>");
        }

        // Extra pages link to /{name}.html
        foreach (var page in _otherPages)
        {
            var name = Path.GetFileNameWithoutExtension(page);
            sb.AppendLine($"  <li><a href=\"/{name}.html\">{name}</a></li>");
        }

        sb.AppendLine("</ul></nav>");
        return sb.ToString();
    }
    

    private Result GenerateIndex(string indexFile, string navHtml)
    {
        var indexPath = Path.Combine(_inputDir, indexFile);
        if (!File.Exists(indexPath))
        {
            return Result.Fail(new Error($"Index file not found: {indexPath}"));
        }

        var rawMarkdown = File.ReadAllText(indexPath);
        var bodyHtml = Markdown.ToHtml(rawMarkdown, _markdownPipeline);

        // Extract plain-text description for OG
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
    

    private Result GeneratePostsSection(string sectionName, string navHtml)
    {
        var sectionInput = Path.Combine(_inputDir, sectionName);
        if (!Directory.Exists(sectionInput))
        {
            Console.WriteLine($"Warning: no folder for section '{sectionName}' at {sectionInput}. Skipping.");
            return Result.Ok();
        }

        // --- 1. Load and parse all posts ---
        var postFiles = Directory.GetFiles(sectionInput, "*.md", SearchOption.TopDirectoryOnly);
        var posts = new List<PostEntry>();

        foreach (var file in postFiles)
{
    var raw = File.ReadAllText(file);
    var (date, parsedTitle, cleanMarkdown) = ExtractAndStripDateHeader(raw);
    var htmlContent = Markdown.ToHtml(cleanMarkdown, _markdownPipeline);
    var fileName = Path.GetFileNameWithoutExtension(file);

    // Prefer the parsed title; fall back to the filename when there is none.
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

        // --- 2. Sort: dated first (newest first), then undated ---
        var sorted = posts
            .OrderByDescending(p => p.Date.HasValue)
            .ThenByDescending(p => p.Date ?? DateTime.MinValue)
            .ThenBy(p => p.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // --- 3. Paginate ---
        var pages = PaginationHelper.Paginate(sorted, _postsPerPage);
        var totalPages = pages.Count;

        // --- 4. Write each section listing page ---
        for (int i = 0; i < totalPages; i++)
        {
            var pageNumber = i + 1;
            var fileName = PaginationHelper.GetPageFileName(sectionName, pageNumber);
            var outPath = Path.Combine(_outputDir, fileName);

            var bodyBuilder = new StringBuilder();
            bodyBuilder.AppendLine($"<section class=\"post-list\">");
            bodyBuilder.AppendLine($"  <h1>{sectionName}</h1>");

            if (_rssIcon && _rssFeed)
            {
                var iconUrl = ResolveRssIconUrl();
                if (!string.IsNullOrEmpty(iconUrl))
                {
                    bodyBuilder.AppendLine(
                        $"  <a class=\"rss-link\" href=\"/{sectionName}.xml\">" +
                        $"<img src=\"{iconUrl}\" alt=\"RSS Feed\" /></a>");
                }
            }

            if (pages[i].Count == 0)
            {
                bodyBuilder.AppendLine("  <p>No posts yet.</p>");
            }
            else
            {
                foreach (var post in pages[i])
                {
                    bodyBuilder.AppendLine("  <article class=\"post-summary\">");
                    bodyBuilder.AppendLine(
                        $"    <h2><a href=\"/{sectionName}/{post.FileName}.html\">" +
                        $"{System.Net.WebUtility.HtmlEncode(post.Title)}</a></h2>");

                    if (post.Date.HasValue)
                    {
                        bodyBuilder.AppendLine(
                            $"    <p class=\"post-date\">{post.Date.Value:yyyy-MM-dd}</p>");
                    }

                    bodyBuilder.AppendLine(
                        $"    <p>{System.Net.WebUtility.HtmlEncode(post.Summary)}</p>");
                    bodyBuilder.AppendLine("  </article>");
                }
            }

            bodyBuilder.AppendLine("</section>");

            // Pagination links
            var paginationHtml = PaginationHelper.GeneratePaginationLinks(
                sectionName, pageNumber, totalPages, _baseUrl);
            bodyBuilder.AppendLine(paginationHtml);

            var bodyHtml = bodyBuilder.ToString();

            // OG data for this listing page
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

        // --- 5. Write individual post pages ---
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
                bodyHtml: $"<article class=\"post\">{post.HtmlContent}</article>",
                ogData: ogData,
                extraHeadTags: string.Empty);

            File.WriteAllText(postPath, fullHtml);
            Console.WriteLine($"Generated {postPath}");
        }

        // --- 6. RSS feed (unpaginated, contains all posts) ---
        if (_rssFeed)
        {
            WriteRssFeed(sectionName, sorted);
        }

        return Result.Ok();
    }

 
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
    

    private string BuildFullHtmlDocument(
        string title,
        string navHtml,
        string bodyHtml,
        OpenGraphData? ogData,
        string extraHeadTags)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\" />");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.AppendLine($"  <title>{System.Net.WebUtility.HtmlEncode(title)}</title>");

        if (!string.IsNullOrEmpty(_css))
        {
            sb.AppendLine($"  <link rel=\"stylesheet\" href=\"/css/{_css}\" />");
        }
        else
        {
            sb.AppendLine("  <link rel=\"stylesheet\" href=\"/css/site.css\" />");
        }

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
        sb.AppendLine("<header class=\"site-header\">");
        sb.AppendLine($"  <h1 class=\"site-title\"><a href=\"/\">{System.Net.WebUtility.HtmlEncode(_siteTitle)}</a></h1>");
        sb.AppendLine(navHtml);
        sb.AppendLine("</header>");
        sb.AppendLine("<main>");
        sb.AppendLine(bodyHtml);
        sb.AppendLine("</main>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }
    

    private void WriteRssFeed(string sectionName, List<PostEntry> posts)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<rss version=\"2.0\">");
        sb.AppendLine("  <channel>");
        sb.AppendLine($"    <title>{System.Net.WebUtility.HtmlEncode(_siteTitle)} - {sectionName}</title>");
        sb.AppendLine($"    <link>{_baseUrl}/{sectionName}.html</link>");
        sb.AppendLine($"    <description>Latest {sectionName} posts</description>");

        foreach (var post in posts.Take(50))
        {
            var link = $"{_baseUrl}/{sectionName}/{post.FileName}.html";
            var pubDate = (post.Date ?? DateTime.UtcNow).ToString("R");

            sb.AppendLine("    <item>");
            sb.AppendLine($"      <title>{System.Net.WebUtility.HtmlEncode(post.Title)}</title>");
            sb.AppendLine($"      <link>{link}</link>");
            sb.AppendLine($"      <guid>{link}</guid>");
            sb.AppendLine($"      <pubDate>{pubDate}</pubDate>");
            sb.AppendLine($"      <description>{System.Net.WebUtility.HtmlEncode(post.Summary)}</description>");
            sb.AppendLine("    </item>");
        }

        sb.AppendLine("  </channel>");
        sb.AppendLine("</rss>");

        var outPath = Path.Combine(_outputDir, $"{sectionName}.xml");
        File.WriteAllText(outPath, sb.ToString());
        Console.WriteLine($"Generated {outPath}");
    }
    

    private static (DateTime? date, string title, string cleanMarkdown) ExtractAndStripDateHeader(string markdown)
{
    // Match a leading H1 of the form: "# YYYY-MM-DD Some Title"
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

    // No date header: try to derive a title from an H1 if present, else null.
    var h1 = Regex.Match(markdown, @"^#\s+(.+)$", RegexOptions.Multiline);
    var fallbackTitle = h1.Success ? h1.Groups[1].Value.Trim() : null;

    return (null, fallbackTitle, markdown)!;
}

    private static string DeriveTitle(string markdown)
    {
        // Prefer an H1 that isn't a date
        var h1 = Regex.Match(markdown, @"^#\s+(.+)$", RegexOptions.Multiline);
        if (h1.Success && !Regex.IsMatch(h1.Groups[1].Value, @"^\d{4}-\d{2}-\d{2}"))
        {
            return h1.Groups[1].Value.Trim();
        }

        // Fall back to the first non-empty line
        var firstLine = markdown
            .Split('\n')
            .Select(l => l.Trim())
            .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));

        return firstLine!;
    }

    private string ResolveDefaultOgImageUrl()
    {
        var candidates = new[]
        {
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

        // Fallback: still provide a URL so OG tags are always present
        return $"{_baseUrl}/images/og-image.png";
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
