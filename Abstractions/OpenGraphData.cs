using System.Text;

namespace Markerator.Abstractions
{
    public class OpenGraphData
    {
        public string Title { get; init; } = null!;
        public string Description { get; init; } = null!;
        public string ImageUrl { get; init; } = null!;
        public string Url { get; init; } = null!;
        public string Type { get; init; } = "website";
        public string SiteName { get; init; } = null!;

        // Optional Twitter Card specific
        private string TwitterCard { get; set; } = "summary_large_image";
        private string TwitterSite { get; set; } = null!;

        public string GenerateMetaTags()
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(Title))
            {
                sb.AppendLine($"<meta property=\"og:title\" content=\"{EscapeHtml(Title)}\" />");
            }

            if (!string.IsNullOrEmpty(Description))
            {
                sb.AppendLine($"<meta property=\"og:description\" content=\"{EscapeHtml(Description)}\" />");
            }

            if (!string.IsNullOrEmpty(ImageUrl))
            {
                sb.AppendLine($"<meta property=\"og:image\" content=\"{EscapeHtml(ImageUrl)}\" />");
                sb.AppendLine("<meta property=\"og:image:width\" content=\"1200\" />");
                sb.AppendLine("<meta property=\"og:image:height\" content=\"630\" />");
            }

            if (!string.IsNullOrEmpty(Url))
            {
                sb.AppendLine($"<meta property=\"og:url\" content=\"{EscapeHtml(Url)}\" />");
            }

            if (!string.IsNullOrEmpty(Type))
            {
                sb.AppendLine($"<meta property=\"og:type\" content=\"{EscapeHtml(Type)}\" />");
            }

            if (!string.IsNullOrEmpty(SiteName))
            {
                sb.AppendLine($"<meta property=\"og:site_name\" content=\"{EscapeHtml(SiteName)}\" />");
            }

            // Twitter Card tags for better cross-platform support
            if (!string.IsNullOrEmpty(TwitterCard))
            {
                sb.AppendLine($"<meta name=\"twitter:card\" content=\"{EscapeHtml(TwitterCard)}\" />");
            }

            if (!string.IsNullOrEmpty(Title))
            {
                sb.AppendLine($"<meta name=\"twitter:title\" content=\"{EscapeHtml(Title)}\" />");
            }

            if (!string.IsNullOrEmpty(Description))
            {
                sb.AppendLine($"<meta name=\"twitter:description\" content=\"{EscapeHtml(Description)}\" />");
            }

            if (!string.IsNullOrEmpty(ImageUrl))
            {
                sb.AppendLine($"<meta name=\"twitter:image\" content=\"{EscapeHtml(ImageUrl)}\" />");
            }

            if (!string.IsNullOrEmpty(TwitterSite))
            {
                sb.AppendLine($"<meta name=\"twitter:site\" content=\"{EscapeHtml(TwitterSite)}\" />");
            }

            return sb.ToString();
        }

        private static string EscapeHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return System.Net.WebUtility.HtmlEncode(input);
        }
    }
}
