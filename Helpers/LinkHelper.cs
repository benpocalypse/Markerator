using System;
using HtmlAgilityPack;

namespace Markerator.Helpers
{
    /// <summary>
    /// Post-processes rendered HTML to ensure relative links (images, anchors, etc.)
    /// are prefixed with "./" so they resolve correctly when the HTML file is 
    /// opened directly from the local file system.
    /// </summary>
    public static class LinkHelper
    {
        /// <summary>
        /// Rewrites relative href and src attributes to start with "./".
        /// </summary>
        /// <param name="html">The rendered HTML fragment.</param>
        /// <returns>The HTML with relative paths prefixed.</returns>
        public static string MakeLinksFileSystemFriendly(string html)
        {
            if (string.IsNullOrEmpty(html)) return html;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Elements that commonly use href
            foreach (var node in doc.DocumentNode.SelectNodes("//a[@href]|//link[@href]") ?? System.Linq.Enumerable.Empty<HtmlNode>())
            {
                var originalHref = node.GetAttributeValue("href", string.Empty);
                var newHref = PrefixRelativeUrl(originalHref);
                if (newHref != originalHref)
                {
                    node.SetAttributeValue("href", newHref);
                }
            }

            // Elements that commonly use src
            foreach (var node in doc.DocumentNode.SelectNodes("//img[@src]|//script[@src]") ?? System.Linq.Enumerable.Empty<HtmlNode>())
            {
                var originalSrc = node.GetAttributeValue("src", string.Empty);
                var newSrc = PrefixRelativeUrl(originalSrc);
                if (newSrc != originalSrc)
                {
                    node.SetAttributeValue("src", newSrc);
                }
            }

            return doc.DocumentNode.OuterHtml;
        }

        private static string PrefixRelativeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;

            // Trim whitespace
            url = url.Trim();

            // If it already starts with "./", "../", "/", "#", or a protocol (http:, https:, mailto:, etc.)
            // we leave it alone.
            if (url.StartsWith("./") || 
                url.StartsWith("../") || 
                url.StartsWith("/") || 
                url.StartsWith("#") || 
                url.Contains(":") /* catches http:, https:, mailto:, data:, etc. */)
            {
                return url;
            }

            return "./" + url;
        }
    }
}