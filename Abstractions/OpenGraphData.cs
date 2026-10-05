using System;
using System.Text;

namespace Markerator.Abstractions
{
    public class OpenGraphData
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Url { get; set; }
        public string Type { get; set; } = "website";
        public string SiteName { get; set; }

        // Twitter Card
        public string TwitterCard { get; set; } = "summary_large_image";
        public string TwitterSite { get; set; }

        public string GenerateMetaTags()
        {
            var sb = new StringBuilder();

            // ---- Open Graph ----
            if (!string.IsNullOrEmpty(Title))
                sb.AppendLine($"<meta property=\"og:title\" content=\"{EscapeHtml(Title)}\" />");

            if (!string.IsNullOrEmpty(Description))
                sb.AppendLine($"<meta property=\"og:description\" content=\"{EscapeHtml(Description)}\" />");

            if (!string.IsNullOrEmpty(ImageUrl))
            {
                sb.AppendLine($"<meta property=\"og:image\" content=\"{EscapeHtml(ImageUrl)}\" />");
                sb.AppendLine("<meta property=\"og:image:width\" content=\"1200\" />");
                sb.AppendLine("<meta property=\"og:image:height\" content=\"630\" />");
            }

            if (!string.IsNullOrEmpty(Url))
                sb.AppendLine($"<meta property=\"og:url\" content=\"{EscapeHtml(Url)}\" />");

            if (!string.IsNullOrEmpty(Type))
                sb.AppendLine($"<meta property=\"og:type\" content=\"{EscapeHtml(Type)}\" />");

            if (!string.IsNullOrEmpty(SiteName))
                sb.AppendLine($"<meta property=\"og:site_name\" content=\"{EscapeHtml(SiteName)}\" />");

            // ---- Twitter Card (matches old 0.6.0 output, which used value= not content=) ----
            if (!string.IsNullOrEmpty(TwitterCard))
                sb.AppendLine($"<meta name=\"twitter:card\" value=\"{EscapeHtml(TwitterCard)}\" />");

            if (!string.IsNullOrEmpty(Url) && Uri.TryCreate(Url, UriKind.Absolute, out var uri))
                sb.AppendLine($"<meta name=\"twitter:domain\" value=\"{EscapeHtml(uri.Host)}\" />");

            if (!string.IsNullOrEmpty(Title))
                sb.AppendLine($"<meta name=\"twitter:title\" value=\"{EscapeHtml(Title)}\" />");

            if (!string.IsNullOrEmpty(Description))
                sb.AppendLine($"<meta name=\"twitter:description\" value=\"{EscapeHtml(Description)}\" />");

            if (!string.IsNullOrEmpty(ImageUrl))
                sb.AppendLine($"<meta name=\"twitter:image\" value=\"{EscapeHtml(ImageUrl)}\" />");

            if (!string.IsNullOrEmpty(Url))
                sb.AppendLine($"<meta name=\"twitter:url\" value=\"{EscapeHtml(Url)}\" />");

            if (!string.IsNullOrEmpty(TwitterSite))
                sb.AppendLine($"<meta name=\"twitter:site\" value=\"{EscapeHtml(TwitterSite)}\" />");

            return sb.ToString();
        }

        private static string EscapeHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return System.Net.WebUtility.HtmlEncode(input);
        }
    }
}
