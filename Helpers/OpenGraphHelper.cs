using System.Text;
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

            // Work line-by-line so we can grab the first real paragraph and stop.
            var lines = markdownContent.Replace("\r\n", "\n").Split('\n');

            var paragraph = new StringBuilder();
            bool inCodeFence = false;
            bool seenHeading = false;

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd();

                // Skip fenced code blocks entirely.
                if (line.TrimStart().StartsWith("```"))
                {
                    inCodeFence = !inCodeFence;
                    continue;
                }
                if (inCodeFence) continue;

                // Skip headings (but note that we've passed one).
                if (Regex.IsMatch(line, @"^\s*#{1,6}\s"))
                {
                    seenHeading = true;
                    continue;
                }

                // Blank line ends a paragraph.
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (paragraph.Length > 0) break;
                    continue;
                }

                // Skip horizontal rules and images-only lines.
                if (Regex.IsMatch(line, @"^\s*(-{3,}|\*{3,}|_{3,})\s*$")) continue;
                if (Regex.IsMatch(line, @"^\s*!\[[^\]]*\]\([^\)]+\)\s*$")) continue;

                // Accumulate text into the first paragraph.
                paragraph.AppendLine(line);
            }

            var text = paragraph.ToString().Trim();
            if (string.IsNullOrEmpty(text))
            {
                // Fall back: if the whole file is just a heading, use its text.
                var h1 = Regex.Match(markdownContent, @"^[ \t]*#{1,6}[ \t]+(.+)$", RegexOptions.Multiline);
                if (h1.Success) text = h1.Groups[1].Value.Trim();
            }
            if (string.IsNullOrEmpty(text)) return string.Empty;

            // Clean up inline markdown inside the paragraph.
            text = Regex.Replace(text, @"!\[[^\]]*\]\([^\)]+\)", "");         // images
            text = Regex.Replace(text, @"\[([^\]]+)\]\([^\)]+\)", "$1");      // links keep text
            text = Regex.Replace(text, @"`([^`]+)`", "$1");                    // inline code
            text = Regex.Replace(text, @"(\*\*|__|\*|_)", "");                 // emphasis
            text = Regex.Replace(text, @"\s+", " ").Trim();

            if (text.Length <= maxLength) return text;

            // Prefer to end at a sentence boundary near maxLength.
            var cut = text.LastIndexOfAny(new[] { '.', '!', '?' }, maxLength - 1);
            if (cut > maxLength / 2) return text.Substring(0, cut + 1);

            return text.Substring(0, maxLength).TrimEnd() + "...";
        }
    }
}
