using System.Text.RegularExpressions;
using Markerator.Abstractions;

namespace Markerator.Helpers;

/// <summary>
/// Helpers for constructing <see cref="OpenGraphData"/> instances for the
/// different page types Markerator generates, and for deriving plain-text
/// descriptions from markdown content.
/// </summary>
public static class OpenGraphHelper
{
    /// <summary>
    /// Builds Open Graph metadata for the site index page.
    /// </summary>
    /// <param name="title">The site title.</param>
    /// <param name="baseUrl">The site base URL.</param>
    /// <param name="siteName">The site name; falls back to <paramref name="title"/> if null.</param>
    /// <param name="markdownContent">The index markdown, used to derive a description.</param>
    /// <returns>The populated <see cref="OpenGraphData"/>.</returns>
    public static OpenGraphData GenerateForIndex(
        string title, string baseUrl, string siteName, string markdownContent)
    {
        return new OpenGraphData
        {
            Title = title,
            Description = GenerateDescription(markdownContent, 160),
            ImageUrl = $"{baseUrl.TrimEnd('/')}/images/og-image.png",
            Url = baseUrl.TrimEnd('/') + "/",
            Type = "website",
            SiteName = siteName ?? title
        };
    }

    /// <summary>
    /// Builds Open Graph metadata for an individual post page. Strips any
    /// leading <c>YYYY-MM-DD</c> prefix from the title if present.
    /// </summary>
    /// <param name="title">The post title, possibly prefixed with a date.</param>
    /// <param name="baseUrl">The site base URL.</param>
    /// <param name="siteName">The site name.</param>
    /// <param name="markdownContent">The post markdown, used to derive a description.</param>
    /// <param name="postPath">The post's relative output path.</param>
    /// <returns>The populated <see cref="OpenGraphData"/>.</returns>
    public static OpenGraphData GenerateForPost(
        string title, string baseUrl, string siteName,
        string markdownContent, string postPath)
    {
        var cleanTitle = title;
        var dateMatch = Regex.Match(title, @"^(\d{4}-\d{2}-\d{2})\s+(.+)$");
        if (dateMatch.Success)
            cleanTitle = dateMatch.Groups[2].Value.Trim();

        return new OpenGraphData
        {
            Title = cleanTitle,
            Description = GenerateDescription(markdownContent, 160),
            ImageUrl = $"{baseUrl.TrimEnd('/')}/images/og-image.png",
            Url = $"{baseUrl.TrimEnd('/')}/{postPath.Replace('\\', '/')}",
            Type = "article",
            SiteName = siteName ?? cleanTitle
        };
    }

    /// <summary>
    /// Builds Open Graph metadata for a standalone page (About, Contact, etc.).
    /// </summary>
    /// <param name="title">The page title.</param>
    /// <param name="baseUrl">The site base URL.</param>
    /// <param name="siteName">The site name.</param>
    /// <param name="markdownContent">The page markdown, used to derive a description.</param>
    /// <param name="pagePath">The page's relative output path.</param>
    /// <returns>The populated <see cref="OpenGraphData"/>.</returns>
    public static OpenGraphData GenerateForPage(
        string title, string baseUrl, string siteName,
        string markdownContent, string pagePath)
    {
        return new OpenGraphData
        {
            Title = title,
            Description = GenerateDescription(markdownContent, 160),
            ImageUrl = $"{baseUrl.TrimEnd('/')}/images/og-image.png",
            Url = $"{baseUrl.TrimEnd('/')}/{pagePath.Replace('\\', '/')}",
            Type = "website",
            SiteName = siteName ?? title
        };
    }

    /// <summary>
    /// Derives a plain-text description from markdown content for use in
    /// Open Graph metadata. Strips headings, images, code blocks, inline code,
    /// and emphasis markers; takes the first sentence if it fits within
    /// <paramref name="maxLength"/>, otherwise truncates with an ellipsis.
    /// </summary>
    /// <param name="markdownContent">The raw markdown content.</param>
    /// <param name="maxLength">The maximum desired description length.</param>
    /// <returns>The cleaned, truncated description.</returns>
    public static string GenerateDescription(string markdownContent, int maxLength)
    {
        if (string.IsNullOrEmpty(markdownContent)) return string.Empty;

        var cleanText = markdownContent;
        cleanText = Regex.Replace(cleanText, @"^#+\s+.*$", "", RegexOptions.Multiline);
        cleanText = Regex.Replace(cleanText, @"!\[[^\]]*\]\([^\)]+\)", "");
        cleanText = Regex.Replace(cleanText, @"\[([^\]]+)\]\([^\)]+\)", "$1");
        cleanText = Regex.Replace(cleanText, @"```[\s\S]*?```", "");
        cleanText = Regex.Replace(cleanText, @"`[^`]+`", "");
        cleanText = Regex.Replace(cleanText, @"(\*\*|__|\*|_)", "");
        cleanText = Regex.Replace(cleanText, @"\s+", " ").Trim();

        if (string.IsNullOrEmpty(cleanText)) return string.Empty;

        var sentences = Regex.Split(cleanText, @"(?<=[.!?])\s+");
        if (sentences.Length > 0 && sentences[0].Length <= maxLength)
            return sentences[0];

        if (cleanText.Length <= maxLength) return cleanText;

        return cleanText.Substring(0, maxLength).TrimEnd() + "...";
    }

    /// <summary>
    /// Returns the absolute path to the first default Open Graph image found
    /// under <paramref name="inputDirectory"/>, or <c>null</c> if none exists.
    /// </summary>
    /// <param name="inputDirectory">The absolute path to the input directory.</param>
    /// <returns>The path to the image, or <c>null</c>.</returns>
    public static string GetDefaultOgImagePath(string inputDirectory)
    {
        var possiblePaths = new[]
        {
            Path.Combine(inputDirectory, "images", "cardimage.png"),
            Path.Combine(inputDirectory, "images", "og-image.jpg"),
            Path.Combine(inputDirectory, "images", "og-image.png"),
            Path.Combine(inputDirectory, "images", "social.jpg"),
            Path.Combine(inputDirectory, "images", "social.png")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path)) return path;
        }

        return string.Empty;
    }
}
