namespace Markerator.Abstractions;

/// <summary>
/// A record that contains all the pertinent details for an indivdual Post that would go on a Posts/News/Updates
/// page.
/// </summary>
/// <param name="PostFilename">The Markdown file that this post is generated from</param>
/// <param name="PostDate">The date (or a default) that this Post was written on</param>
/// <param name="Title">The Post Title</param>
/// <param name="Summary">The top-level Summary of the Post</param>
/// <param name="Contents">The entire paragraph contents for the Post</param>
public record Post(string PostFilename, DateTime? PostDate, string Title, string? Summary, string Contents);