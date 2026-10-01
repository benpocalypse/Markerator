using System.Text.RegularExpressions;
using Markerator.Abstractions;

namespace Markerator.Helpers;

public static class OpenGraphHelper
{
    public static OpenGraphData GenerateForIndex(
        string title,
        string baseUrl,
        string siteName,
        string markdownContent)
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

    public static OpenGraphData GenerateForPost(
        string title,
        string baseUrl,
        string siteName,
        string markdownContent,
        string postPath)
    {
        var cleanTitle = title;

        // If the title still contains a leading "YYYY-MM-DD " prefix, strip it
        var dateMatch = Regex.Match(title, @"^(\d{4}-\d{2}-\d{2})\s+(.+)$");
        if (dateMatch.Success)
        {
            cleanTitle = dateMatch.Groups[2].Value.Trim();
        }

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

    public static OpenGraphData GenerateForPage(
        string title,
        string baseUrl,
        string siteName,
        string markdownContent,
        string pagePath)
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


    public static string GenerateDescription(string markdownContent, int maxLength)
    {
        if (string.IsNullOrEmpty(markdownContent)) return string.Empty;

        var cleanText = markdownContent;

        // Remove headings
        cleanText = Regex.Replace(cleanText, @"^#+\s+.*$", "", RegexOptions.Multiline);

        // Remove images (before links, since image syntax contains link syntax)
        cleanText = Regex.Replace(cleanText, @"!\[[^\]]*\]\([^\)]+\)", "");

        // Remove links but keep their visible text
        cleanText = Regex.Replace(cleanText, @"\[([^\]]+)\]\([^\)]+\)", "$1");

        // Remove fenced code blocks
        cleanText = Regex.Replace(cleanText, @"```[\s\S]*?```", "");

        // Remove inline code
        cleanText = Regex.Replace(cleanText, @"`[^`]+`", "");

        // Remove emphasis markers
        cleanText = Regex.Replace(cleanText, @"(\*\*|__|\*|_)", "");

        // Collapse whitespace
        cleanText = Regex.Replace(cleanText, @"\s+", " ").Trim();

        if (string.IsNullOrEmpty(cleanText)) return string.Empty;

        // Use the first sentence if it fits, otherwise truncate
        var sentences = Regex.Split(cleanText, @"(?<=[.!?])\s+");
        if (sentences.Length > 0 && sentences[0].Length <= maxLength)
        {
            return sentences[0];
        }

        if (cleanText.Length <= maxLength)
        {
            return cleanText;
        }

        return cleanText.Substring(0, maxLength).TrimEnd() + "...";
    }
    
    
    public static string GetDefaultOgImagePath(string inputDirectory)
    {
        var possiblePaths = new[]
        {
            Path.Combine(inputDirectory, "images", "og-image.jpg"),
            Path.Combine(inputDirectory, "images", "og-image.png"),
            Path.Combine(inputDirectory, "images", "social.jpg"),
            Path.Combine(inputDirectory, "images", "social.png")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return string.Empty;
    }
}
