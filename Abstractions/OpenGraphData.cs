using System;
using System.Text;

namespace Markerator.Abstractions;

/// <summary>
/// Holds the Open Graph and Twitter Card metadata for a single generated page,
/// and renders that metadata as a block of <c>&lt;meta&gt;</c> tags.
/// </summary>
public class OpenGraphData
{
    /// <summary>Gets or sets the page title (<c>og:title</c> / <c>twitter:title</c>).</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Gets or sets the page description (<c>og:description</c> / <c>twitter:description</c>).</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets or sets the absolute URL of the preview image (<c>og:image</c> / <c>twitter:image</c>).</summary>
    public string ImageUrl { get; init; } = string.Empty;

    /// <summary>Gets or sets the absolute URL of the page itself (<c>og:url</c> / <c>twitter:url</c>).</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>Gets or sets the Open Graph content type. Defaults to <c>website</c>.</summary>
    public string Type { get; init; } = "website";

    /// <summary>Gets or sets the site name (<c>og:site_name</c>).</summary>
    public string SiteName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the Twitter Card style. Defaults to <c>summary_large_image</c>.
    /// </summary>
    public string TwitterCard { get; set; } = "summary_large_image";

    /// <summary>Gets or sets the Twitter <c>@site</c> handle to attribute the card to.</summary>
    public string TwitterSite { get; set; } = string.Empty;

    /// <summary>
    /// Renders the Open Graph and Twitter Card meta tags for this page as a
    /// newline-separated string of <c>&lt;meta&gt;</c> elements. Properties with
    /// empty or null values are omitted.
    /// </summary>
    /// <returns>The rendered <c>&lt;meta&gt;</c> tag block.</returns>
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

        // ---- Twitter Card ----
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

    /// <summary>
    /// HTML-encodes the given string for safe inclusion inside an attribute value.
    /// </summary>
    /// <param name="input">The raw string.</param>
    /// <returns>The encoded string, or an empty string if the input is null or empty.</returns>
    private static string EscapeHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
        
        return System.Net.WebUtility.HtmlEncode(input);
    }
}
