using System.Text.RegularExpressions;
using Markerator.Abstractions;

namespace Markerator.Helpers
{
    public static class OpenGraphHelper
    {
        public static OpenGraphData GenerateForIndex(
            string title, string baseUrl, string siteName, string markdownContent)
        {
            return new OpenGraphData
            {
                Title = title,
                Description = GenerateDescription(markdownContent, 160),
                ImageUrl = $"{baseUrl.TrimEnd('/')}/images/cardimage.png",
                Url = baseUrl.TrimEnd('/') + "/",
                Type = "website",
                SiteName = siteName ?? title
            };
        }

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
                ImageUrl = $"{baseUrl.TrimEnd('/')}/images/cardimage.png",
                Url = $"{baseUrl.TrimEnd('/')}/{postPath.Replace('\\', '/')}",
                Type = "article",
                SiteName = siteName ?? cleanTitle
            };
        }

        public static OpenGraphData GenerateForPage(
            string title, string baseUrl, string siteName,
            string markdownContent, string pagePath)
        {
            return new OpenGraphData
            {
                Title = title,
                Description = GenerateDescription(markdownContent, 160),
                ImageUrl = $"{baseUrl.TrimEnd('/')}/images/cardimage.png",
                Url = $"{baseUrl.TrimEnd('/')}/{pagePath.Replace('\\', '/')}",
                Type = "website",
                SiteName = siteName ?? title
            };
        }

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
}
