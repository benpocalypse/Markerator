using System;
using System.Collections.Generic;
using System.Xml;
using System.ServiceModel.Syndication;
using System.IO;

namespace com.github.benpocalypse.markerator;

public static class RssGenerator
{
    public static SyndicationFeed GenerateRssFeed(string feedTitle, string feedDescription, Uri baseUri, IEnumerable<Post> posts)
    {
        SyndicationFeed feed = new SyndicationFeed(feedTitle, feedDescription, baseUri);

        var items = new List<SyndicationItem>();
        
        foreach (var post in posts)
        {
            var item = new SyndicationItem(
                title: post.Title, 
                content: post.Contents,
                itemAlternateLink: new Uri ($@"{baseUri.OriginalString}/{feedTitle}/{post.PostFilename}.html"), // FIXME - This isn't right.
                id: Guid.NewGuid().ToString(),
                lastUpdatedTime: post?.PostDate ?? DateTime.Now);

            item.Categories.Add(new SyndicationCategory("feedTitle"));
            items.Add(item);
        }
        
        feed.Items = items;
        feed.Language = "en-us";
        feed.LastUpdatedTime = DateTime.Now;

        XmlWriter rssWriter = XmlWriter.Create(Path.Combine(Directory.GetCurrentDirectory(), $@"output/{feedTitle}/", $@"{feedTitle}.xml"));
        Rss20FeedFormatter rssFormatter = new Rss20FeedFormatter(feed);
        rssFormatter.WriteTo(rssWriter);
        rssWriter.Close();

        return feed;
    }

    public static bool VerifyRssImageExistsInOutput()
    {
        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "output", "images/rss.png")) ||
            File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "output", "images/rss.jpg"))
           )
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static string GetRssImageFilename()
    {
        var filename = string.Empty;

        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "output", "images/rss.png")))
        {
            filename = "images/rss.png";
        }

        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "output", "images/rss.jpg")))
        {
            filename = "images/rss.jpg";
        }

        return filename;
    }
}