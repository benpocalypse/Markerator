using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using com.github.benpocalypse.markerator.helpers;
using HtmlAgilityPack;
using Markdig;

namespace com.github.benpocalypse.markerator;

public static class HtmlGenerator
{
    public static string CreateHtmlPage(
        string markdownFile,
        bool includeFavicon,
        bool includePosts,
        List<string> postsTitle,
        string siteTitle,
        IReadOnlyList<string> otherPages,
        string css,
        string baseUrl,
        bool isIndex = false)
    {
        string htmlIndex = string.Empty;

        foreach (var page in otherPages)
        {
            Console.WriteLine(@$"Attempting to create page for: {page}.");
        }

        try
        {
            string contentFilename = Path.Combine(Directory.GetCurrentDirectory(), "input", markdownFile);
            string contentMarkdown = File.ReadAllText(contentFilename);
            var contentPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            string contentHtml = Markdown.ToHtml(contentMarkdown, contentPipeline);

            var doc = new HtmlDocument();
            doc.LoadHtml(contentHtml);
            var postHtmlSummary = doc.DocumentNode.SelectNodes("//p")?.First()?.InnerText;


            string metaCards = @$"
<meta property=""og:type"" content=""website""/>
<meta property=""og:url"" content=""{baseUrl}"" />
<meta property=""og:title"" content=""{siteTitle}"" />
<meta property=""og:description"" content=""{postHtmlSummary}"" />
<meta property=""og:image"" content=""{baseUrl}/cardimage.png"" />
<meta name=""twitter:card"" content=""summary_large_image"">
<meta name=""twitter:domain"" value=""{baseUrl}"" />
<meta name=""twitter:title"" value=""{siteTitle}"" />
<meta name=""twitter:description"" value=""{postHtmlSummary}"" />
<meta name=""twitter:image"" content=""{baseUrl}/cardimage.png"" />
<meta name=""twitter:url"" value=""{baseUrl}"" />
";

            htmlIndex = GetPageHtml(
                otherPages: otherPages,
                postsTitle: postsTitle,
                css: css,
                siteTitle: siteTitle,
                html: contentHtml,
                metaCards: metaCards,
                includeFavicon: includeFavicon,
                includePosts: includePosts,
                isPosts: false
            );

        }
        catch(Exception ex)
        {
            return @$"Failed to read markdown file {markdownFile} due to: {ex.Message}";
        }
        finally
        {
            File.WriteAllText(
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "output",
                    isIndex == true ?
                        "index.html" :
                        Path.GetFileNameWithoutExtension(markdownFile) + ".html"
                    ),
                    htmlIndex);
        }

        return @$"Created Html from Markdown file {markdownFile}";
    }

    public static IEnumerable<Post> CreateHtmlPostPages(
        bool includeFavicon,
        string postsTitle,
        string siteTitle,
        IReadOnlyList<string> otherPages,
        string baseUrl,
        bool rss,
        string rssImage,
        string css)
    {
        ImmutableList<Post> postsCollection = ImmutableList<Post>.Empty;

        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "output", postsTitle));

        string postsIndexHtml = @$"<h2>{postsTitle}";

        postsIndexHtml += rss == true ?
            @$"   <a href=""{postsTitle}/{postsTitle}.xml"">
        <img src=""{rssImage}"" alt=""Rss icon"">
    </a>
</h2>" : @$"</h2>";

        var path = Path.Combine(Directory.GetCurrentDirectory(), "input", postsTitle);

        var postFiles = Directory.GetFiles(path);
        
        var postOrder = GetPostOrder(postFiles);
        var postOrderIterator = postOrder.GetEnumerator();

        string previousYear = "All";

        while (postOrderIterator.MoveNext())
        {
            string postMarkdown = File.ReadAllText(postOrderIterator.Current.Value);
            var postPipelineBuilder = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            string postHtml = Markdown.ToHtml(postMarkdown, postPipelineBuilder);
            var postHtmlFile = Path.GetFileNameWithoutExtension(postOrderIterator.Current.Value) + ".html";

            Console.WriteLine($"...adding post for {postOrderIterator.Current.Value}...");

            var doc = new HtmlDocument();
            doc.LoadHtml(postHtml);

            string? postDate = doc.DocumentNode.SelectNodes("//h1")?.First()?.InnerText;
            var postHtmlTitle = doc.DocumentNode.SelectNodes("//h2")?.First()?.InnerText;
            var postHtmlSummary = doc.DocumentNode.SelectNodes("//p")?.First()?.InnerText;
            var paragraphElements = doc.DocumentNode.SelectNodes("//p")?.Elements();
            var postHtmlContents = paragraphElements?.Skip(1).Take(paragraphElements?.Count() ?? 1);

            string contents = string.Empty;
            
            if (postHtmlContents is not null)
            {
                foreach (var post in postHtmlContents)
                {
                    contents += post.InnerText + System.Environment.NewLine;
                }
            }

            var newPost = new Post(Path.GetFileNameWithoutExtension(postOrderIterator.Current.Value), DateTime.Parse(postDate ?? DateTime.Now.ToString()) , postHtmlTitle ?? string.Empty, postHtmlSummary ?? string.Empty, contents);
            postsCollection = postsCollection.Add(newPost);

            string currentYear;

            if (DateTime.TryParse(postDate, out var postDateTime) && !postDateTime.Equals(DateTime.MinValue))
            {
                currentYear = postDateTime.ToString("yyyy");
            }
            else
            {
                currentYear = "All";
            }

            if (currentYear != previousYear)
            {
                postsIndexHtml += @$"<h3>{currentYear}</h3>
                ";
               previousYear = currentYear;
            }

            // TODO - Maybe? support images/cards for post summaries, or perhaps some sort of custom formatting?
            //      - Or allow some CLI options to show summaries under links, etc?
            postsIndexHtml += @$"&emsp;<a href=""{postsTitle}/{postHtmlFile}"">{(!postDateTime.Equals(DateTime.MinValue) ? postDateTime.ToString("MM/dd") + " - " : string.Empty)}{postHtmlTitle}</a><br/>
&emsp;{postHtmlSummary}
<br/>
<br/>
";

            string metaCards = @$"
<meta property=""og:type"" content=""website""/>
<meta property=""og:url"" content=""{baseUrl}"" />
<meta property=""og:title"" content=""{siteTitle}"" />
<meta property=""og:description"" content=""{postHtmlSummary}"" />
<meta property=""og:image"" content=""{baseUrl}/cardimage.png"" />
<meta name=""twitter:card"" content=""summary_large_image"">
<meta name=""twitter:domain"" value=""{baseUrl}"" />
<meta name=""twitter:title"" value=""{siteTitle}"" />
<meta name=""twitter:description"" value=""{postHtmlSummary}"" />
<meta name=""twitter:image"" content=""{baseUrl}/cardimage.png"" />
<meta name=""twitter:url"" value=""{baseUrl}"" />
<meta name=""twitter:label1"" value=""Posted:"" />
<meta name=""twitter:data1"" value=""{(!postDateTime.Equals(DateTime.MinValue) ? postDateTime.ToString("MM/dd") : string.Empty)}"" />
";


            var htmlPost = GetPageHtml(
                otherPages: otherPages,
                postsTitle: new List<string>() {postsTitle},
                css: css,
                siteTitle: siteTitle,
                html: postHtml,
                includeFavicon: includeFavicon,
                metaCards: metaCards,
                includePosts: true,
                isPosts: true
            );

            File.WriteAllText(
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "output",
                    $@"{postsTitle}",
                    postHtmlFile
                    ),
                htmlPost);
        }

        var htmlPosts = GetPageHtml(
            otherPages: otherPages,
            postsTitle: new List<string>() {postsTitle},
            css: css,
            siteTitle: siteTitle,
            html: postsIndexHtml,
            metaCards: baseUrl,
            includeFavicon: includeFavicon,
            includePosts: true,
            isPosts: false
        );

        File.WriteAllText(
            Path.Combine(
                Directory.GetCurrentDirectory(),
                "output",
                $@"{postsTitle}.html"
                ),
            htmlPosts);

        return postsCollection as IEnumerable<Post>;
    }

    private static string GetPageHtml(
        IReadOnlyList<string> otherPages,
        List<string> postsTitle,
        string css,
        string siteTitle = "",
        string html = "",
        string metaCards = "",
        bool includeFavicon = false,
        bool includePosts = false,
        bool isPosts = false
    )
    {
        return 
@$"<!DOCTYPE html>
<html>
    <head>
        {metaCards}
        <style>
            {GetFontCss(isPosts: isPosts)}
            {css}
        </style>
        <title>{siteTitle}</title>
        {(includeFavicon is true ?
@$"     <link rel=""icon"" type=""image/x-icon"" href=""images/favicon.ico"">" : string.Empty)}
    </head>
    <body>
        {GetNavigationHtml(
            siteTitle: siteTitle,
            css: css,
            includePosts: includePosts,
            postsTitle: postsTitle,
            otherPages: otherPages,
            isPosts: isPosts)}
        {GetThemeMenuHtml(new List<string>())}
        <div class=""content"">
            {html}
        </div>
        {GetFooterHtml()}
    </body>
</html> ";
    }

    private static string GetNavigationHtml(
        IReadOnlyList<string> otherPages,
        string css,
        string siteTitle,
        bool includePosts,
        List<string> postsTitle,
        bool isPosts)
    {
        var otherPagesHtml = string.Empty;

        foreach (var page in otherPages)
        {
            otherPagesHtml += @$"         <a href=""{(isPosts ==  true ? ".." : ".")}/{Path.GetFileNameWithoutExtension(page)}.html"">{Path.GetFileNameWithoutExtension(page)}</a>
";
        }

        var postsHtml = string.Empty;

        foreach (var posts in postsTitle)
        {
            postsHtml += @$"<a href=""{(isPosts ==  true ? ".." : ".")}/{posts}.html"">{posts}</a>" + System.Environment.NewLine;
        }

        var resultHtml =
            @$"
{(
@$" <div class=""navigation-title"">
            <a href=""{(isPosts ==  true ? ".." : ".")}/index.html"">{siteTitle}</a>
    </div>
    <div class=""navigation"">
{(includePosts == true ?
@$"
            {postsHtml}
            {otherPagesHtml}
            {GetThemeMenuHtml(new List<string>())}
        </div>
" : @$"
        {otherPagesHtml}
        {GetThemeMenuHtml(new List<string>())}
        </div>")}
")}";

        return resultHtml;
    }

    private static string GetFontCss(bool isPosts)
    => @$"
@font-face {{
    font-family: Inconsolata; src: url(""{(isPosts ==  true ? ".." : ".")}/fonts/Inconsolata-Regular.ttf"");
}}
    body {{
         font-family: Inconsolata
}}
";

    private static string GetThemeMenuHtml(IReadOnlyList<string> themeNames)
    {
        string resultHtml = string.Empty;

        if (themeNames.Count > 0 && !themeNames[0].Equals("default"))
        {
            resultHtml = @$"
        <div class=""dropdown"">
            <button class=""dropdownbutton"">Theme</button>
            <div class=""dropdown-content"">
";

            foreach (var theme in themeNames)
            {
                resultHtml += @$"<a href=""index-{theme}.html"">{theme}</a>
";
            }

            resultHtml += @$"            </div>
        </div>
        ";
        }

        return resultHtml;
    }

    private static string GetFooterHtml()
    =>
@$"
    <footer>
        <p>Site generated with <a href=""https://github.com/benpocalypse/Markerator"">Markerator v{Globals.Version}</a>.</p>
    </footer>
";

    private static IEnumerable<KeyValuePair<DateTime, string>> GetPostOrder(string[] postFiles)
    {
        var comparer = new DuplicateKeyComparer<DateTime>();
        var postOrder = new SortedDictionary<DateTime, string>(comparer);

        foreach (var postfile in postFiles)
        {
            string postMarkdown = File.ReadAllText(postfile);
            var postPipelineBuilder = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            string postHtml = Markdown.ToHtml(postMarkdown, postPipelineBuilder);

            var doc = new HtmlDocument();
            doc.LoadHtml(postHtml);

            string? postDate = doc.DocumentNode.SelectNodes("//h1")?.First()?.InnerText;

            if (postDate is not null && DateTime.TryParse(postDate, out var postDateTime))
            {
                postOrder.Add(postDateTime, postfile);
            }
            else
            {
                postOrder.Add(DateTime.MinValue, postfile);
            }
        }

        return postOrder.Reverse();
    }
}
