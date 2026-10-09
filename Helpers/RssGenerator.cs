using System.ServiceModel.Syndication;
using System.Xml;
using Markerator.Abstractions;

namespace Markerator.Helpers; 
/// <summary>
/// The RssGenereator class handles generating the XML files that correspond to the Html conten that Markerator
/// makes. This will allow a user to use an RSS reader to subscribe to the generated site. 
/// </summary>
public static class RssGenerator
{
    /// <summary>
    /// This function does the nuts and bolts of generating the RSS Xml.
    /// </summary>
    /// <param name="feedTitle">The wesbite/feed title that will appear in the XML</param>
    /// <param name="feedDescription">The website/feed description</param>
    /// <param name="baseUri">The URL of the website</param>
    /// <param name="posts">The contents of all the posts/entries for the generated site</param>
    /// <returns></returns>
    public static SyndicationFeed GenerateRssFeed(string feedTitle, string feedDescription, Uri baseUri, IEnumerable<Post> posts)
    {
        SyndicationFeed feed = new SyndicationFeed(feedTitle, feedDescription, baseUri);

        var items = new List<SyndicationItem>();
        
        foreach (var post in posts)
        {
            var item = new SyndicationItem(
                title: post.Title, 
                content: post.Contents,
                itemAlternateLink: new Uri ($@"{baseUri.OriginalString}/{feedTitle}/{post.PostFilename}.html"), // FIXME - This isn't right?
                id: Guid.NewGuid().ToString(),
                lastUpdatedTime: post?.PostDate ?? DateTime.Now);

            item.Categories.Add(new SyndicationCategory("feedTitle"));
            items.Add(item);
        }
        
        feed.Items = items;
        feed.Language = "en-us"; //TODO - Consider allowing configurable languages
        feed.LastUpdatedTime = DateTime.Now;

        XmlWriter rssWriter = XmlWriter.Create(Path.Combine(Directory.GetCurrentDirectory(), $@"output/{feedTitle}/", $@"{feedTitle}.xml"));
        Rss20FeedFormatter rssFormatter = new Rss20FeedFormatter(feed);
        rssFormatter.WriteTo(rssWriter);
        rssWriter.Close();

        return feed;
    }

    /// <summary>
    /// This function checks to make sure that a file named either "rss.png" or "rss.jpg" exists in the
    /// output/images/ folder. Otherwise, the RSS generation will fail if this image doesn't exist.
    /// </summary>
    /// <returns>True if the file exists, False if it does not</returns>
    public static bool VerifyRssImageExistsInOutput()
    {
        (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "output", "images/rss.png")) ||
         File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "output", "images/rss.jpg")))
            .IfTrue(() => Console.WriteLine("Successfully found the RSS image."))
            .OrElse(() =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("ERROR: Failed to find either rss.png or rss.jpg.");
                Console.ResetColor();
            });

        return
            (
                File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "output", "images/rss.png")) ||
                File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "output", "images/rss.jpg"))
            ).IfTrue();
    }

    /// <summary>
    /// This function attempts to find a valid RSS image file and return it's name, otherwise, just return empty.
    /// </summary>
    /// <returns>The RSS image filename.</returns>
    public static string GetRssImageFilename()
    {
        var filename = string.Empty;

        File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "output", "images/rss.png"))
            .IfTrue(() => filename = "images/rss.png")
            .IfFalse(() => filename = "images/rss.jpg")
            .OrElse(() => filename = string.Empty);

        return filename;
    }
}
