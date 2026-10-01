namespace Markerator.Abstractions;

public record Post(string PostFilename, DateTime? PostDate, string Title, string? Summary, string Contents);
