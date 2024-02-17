using System;

namespace com.github.benpocalypse.markerator;

public record Post(string PostFilename, DateTime? PostDate, string Title, string? Summary, string Contents);
