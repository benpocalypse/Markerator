namespace Markerator.Abstractions;

public class PostEntry
{
    public string SourceFile { get; set; } = null!;
    public string FileName { get; init; } = null!;
    public string Title { get; init; } = null!;
    public DateTime? Date { get; init; } = null!;
    public string RawMarkdown { get; set; } = null!;
    public string HtmlContent { get; init; } = null!;
    public string Summary { get; init; } = null!;
}