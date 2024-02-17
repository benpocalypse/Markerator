using System;
using System.IO;
using Markdig;
using FluentArgs;
using FluentResults;
using HtmlAgilityPack;
using System.Linq;
using System.Collections.Generic;
using com.github.benpocalypse.markerator.helpers;
using System.Collections.Immutable;

namespace com.github.benpocalypse.markerator;

public partial class Markerator
{
    private readonly static string _version = "0.3.0";

    static void Main(string[] args)
    {
        FluentArgsBuilder.New()
            .DefaultConfigsWithAppDescription(@$"Markerator v{_version}.
A very simple static website generator written in C#/.Net")
            .RegisterHelpFlag("-h", "--help")
            .Parameter<string>("-t", "--title")
                .WithDescription("The title of the website.")
                .WithExamples("Markerator Generated Site", "zombo.com")
                .IsRequired()
            .Parameter<Uri>("-u", "--url")
                .WithDescription("The base Url of the website, omitting the trailing slash.")
                .WithExamples("https://www.slashdot.org", "https://elementary.io")
                .IsRequired()
            .Parameter<string>("-i", "--indexFile")
                .WithDescription("The markdown file that is to be converted into the index.html file.")
                .WithExamples("mainFile.md", "radicalText.md")
                .WithValidation(name => !name.Contains(" "), name => "Markdown filename cannot contain spaces.")
                .IsRequired()
            .Parameter<bool>("-p", "--posts")
                .WithDescription("Whether or not the site should include a posts link (like a news or updates section.)")
                .WithExamples("true", "false")
                .IsOptionalWithDefault(false)
            .Parameter<string>("-pt", "--postsTitle")
                .WithDescription("The title that the posts section should use.")
                .WithExamples("News", "Updates", "Blog")
                .IsOptionalWithDefault("Posts")
            .Parameter<bool>("-rss", "-rssFeed")
                .WithDescription("Whether or not to generate Rss feeds from your posts/news/blog pages.")
                .WithExamples("true", "false")
                .IsOptionalWithDefault(false)
            .Parameter<bool>("-f", "--favicon")
                .WithDescription("Whether or not the site should use a favicon.ico file in the /input/images directory.")
                .WithExamples("true", "false")
                .IsOptionalWithDefault(false)
            .ListParameter<string>("-op", "--otherPages")
                .WithDescription("Additional pages that should be linked from the navigation bar, provided as a comma separated list of .md files.")
                .WithExamples("About.md,Contact.md")
                .IsOptionalWithDefault(new List<string>())
            .Parameter<string>("-c", "--css")
                .WithDescription("Inlude a custom CSS file that will theme the generated site.")
                .WithExamples("LightTheme.css", "DarkTheme.css")
                .IsOptionalWithDefault("")
            .Call(customCss => otherPages => favicon => rss => postsTitle => posts => indexFile => baseUrl => siteTitle =>
            {
                Console.WriteLine($"Creating site {siteTitle} with index of {indexFile}, including posts: {posts}...");

                CreateOutputDirectories();

                var css = CssValidator.ValidateAndGetCustomCssContents(customCss);

                css.IsFailed.IfTrue(() =>
                {
                    Console.WriteLine("Failed to parse custom css, using default css instead.");
                    css = Result.Ok(DefaultCss);
                });

                // Create index.html
                Console.WriteLine(
                    CreateHtmlPage(
                        otherPages: otherPages,
                        markdownFile: indexFile,
                        includeFavicon: favicon,
                        includePosts: posts,
                        postsTitle: postsTitle,
                        siteTitle: siteTitle,
                        css: css.Value,
                        isIndex: true)
                    );

                // Now add all the other pages, if there are any.
                otherPages.IfNotEmpty(() =>
                {
                    foreach (var page in otherPages)
                    {
                        Console.WriteLine(
                            CreateHtmlPage(
                                otherPages: otherPages,
                                markdownFile: page,
                                includeFavicon: favicon,
                                includePosts: posts,
                                postsTitle: postsTitle,
                                siteTitle: siteTitle,
                                css: css.Value,
                                isIndex: false)
                            );
                    }
                });

                // ...and if there are any "news/posts}/projects" pages, add those as well.
                posts.IfTrue(() =>
                {
                    var postCollection = CreateHtmlPostPages(
                        includeFavicon: favicon,
                        postsTitle: postsTitle,
                        siteTitle: siteTitle,
                        otherPages: otherPages,
                        css: css.Value
                    );

                    RssGenerator.GenerateRssFeed(postsTitle, "This is a test", baseUrl, postCollection);
                });

                Console.WriteLine($"...site generation successful.");
            })
            .Parse(args);
    }

    private static string CreateHtmlPage(
        string markdownFile,
        bool includeFavicon,
        bool includePosts,
        string postsTitle,
        string siteTitle,
        IReadOnlyList<string> otherPages,
        string css,
        bool isIndex = false)
    {
        string htmlIndex = string.Empty;

        try
        {
            string contentFilename = Path.Combine(Directory.GetCurrentDirectory(), "input", markdownFile);
            string contentMarkdown = File.ReadAllText(contentFilename);
            var contentPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            string contentHtml = Markdown.ToHtml(contentMarkdown, contentPipeline);


            htmlIndex = GetPageHtml(
                otherPages: otherPages,
                siteTitle: siteTitle,
                html: contentHtml,
                includeFavicon: includeFavicon,
                includePosts: includePosts,
                postsTitle: postsTitle,
                isPosts: false,
                css: css
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

    private static IEnumerable<Post> CreateHtmlPostPages(
        bool includeFavicon,
        string postsTitle,
        string siteTitle,
        IReadOnlyList<string> otherPages,
        string css)
    {
        ImmutableList<Post> postsCollection = ImmutableList<Post>.Empty;

        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "output", postsTitle));

        string postsIndexHtml = @$"<h2>{postsTitle}</h2>" + System.Environment.NewLine;

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

            postsCollection = postsCollection.Add(new Post(Path.GetFileNameWithoutExtension(postOrderIterator.Current.Value), DateTime.Parse(postDate ?? DateTime.Now.ToString()) , postHtmlTitle ?? string.Empty, postHtmlSummary ?? string.Empty, contents));

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

// TODO - Implement this
// <p>{postHtmlSummary}</p>

            var htmlPost = GetPageHtml(
                otherPages: otherPages,
                siteTitle: siteTitle,
                html: postHtml,
                includeFavicon: includeFavicon,
                includePosts: true,
                postsTitle: postsTitle,
                isPosts: true,
                css: css
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
            siteTitle: siteTitle,
            html: postsIndexHtml,
            includeFavicon: includeFavicon,
            includePosts: true,
            postsTitle: postsTitle,
            isPosts: false,
            css: css
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

    private static void CreateOutputDirectories()
    {
        // TODO: Spit out a message about what the actual proper directory structure for input should look like.
        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "output"));

        if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "input", "images")))
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "output", "images"));
            CopyDirectory(
                sourceDirectory: Path.Combine(Directory.GetCurrentDirectory(), "input", "images"),
                targetDirectory: Path.Combine(Directory.GetCurrentDirectory(), "output", "images")
            );
        }

        if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "input", "fonts")))
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "output", "fonts"));
            CopyDirectory(
                sourceDirectory: Path.Combine(Directory.GetCurrentDirectory(), "input", "fonts"),
                targetDirectory: Path.Combine(Directory.GetCurrentDirectory(), "output", "fonts")
            );
        }
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        var diSource = new DirectoryInfo(sourceDirectory);
        var diTarget = new DirectoryInfo(targetDirectory);

        CopyAll(diSource, diTarget);
    }

    private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);

        foreach (FileInfo fi in source.GetFiles())
        {
            Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
        }

        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir =
                target.CreateSubdirectory(diSourceSubDir.Name);
            CopyAll(diSourceSubDir, nextTargetSubDir);
        }
    }

    private static string GetPageHtml(
        IReadOnlyList<string> otherPages,
        string css,
        string siteTitle = "",
        string html = "",
        bool includeFavicon = false,
        bool includePosts = false,
        string postsTitle = "",
        bool isPosts = false
    ) =>
@$"<!DOCTYPE html>
<html>
    <head>
        <style>
            {GetFontCss(isPosts: isPosts)}
            {css}
        </style>
        <title>{siteTitle}</title>
        {(includeFavicon is true ?
@$"     <link rel=""icon"" type=""image/x-icon"" href=""images/favicon.ico"">" : string.Empty)}
        {GetNavigationHtml(
            siteTitle: siteTitle,
            css: css,
            includePosts: includePosts,
            postsTitle: postsTitle,
            otherPages: otherPages,
            isPosts: isPosts)}
        {GetThemeMenuHtml(new List<string>())}
    </head>
    <body>
        <div class=""content"">
            {html}
        </div>
    </body>
    {GetFooterHtml()}
</html> ";

    private static string GetNavigationHtml(
        IReadOnlyList<string> otherPages,
        string css,
        string siteTitle,
        bool includePosts,
        string postsTitle,
        bool isPosts)
    {
        var otherPagesHtml = string.Empty;

        foreach (var page in otherPages)
        {
            otherPagesHtml += @$"         <a href=""{(isPosts ==  true ? ".." : ".")}/{Path.GetFileNameWithoutExtension(page)}.html"">{Path.GetFileNameWithoutExtension(page)}</a>
";
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
            <a href=""{(isPosts ==  true ? ".." : ".")}/posts.html"">{postsTitle}</a>
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
        <p>Site generated with <a href=""https://github.com/benpocalypse/Markerator"">Markerator v{_version}</a>.</p>
    </footer>
";
}
