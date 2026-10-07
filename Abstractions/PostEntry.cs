namespace Markerator.Abstractions;


/// <summary>
/// Represents a single post parsed from a markdown file in a post section
/// (News, Blog, Projects, etc.), including its parsed metadata and rendered HTML.
/// </summary>
public class PostEntry
{
    /// <summary>Gets or sets the absolute path to the source markdown file.</summary>
    public string SourceFile { get; set; } = string.Empty;

    /// <summary>Gets or sets the file name without extension, used to build the output URL.</summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>Gets or sets the display title of the post.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Gets or sets the parsed publication date, or <c>null</c> for undated posts.</summary>
    public DateTime? Date { get; init; }

    /// <summary>Gets or sets the raw markdown source of the post.</summary>
    public string RawMarkdown { get; set; } = string.Empty;

    /// <summary>Gets or sets the post body rendered to HTML by Markdig.</summary>
    public string HtmlContent { get; init; } = string.Empty;

    /// <summary>Gets or sets the plain-text summary used for the listing and OG description.</summary>
    public string Summary { get; init; } = string.Empty;
}
