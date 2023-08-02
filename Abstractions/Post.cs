using System;

namespace com.github.benpocalypse.markerator.abstractions;

public record Post(DateTime? PostDate, string Title, string? Summary, string Contents);