using FluentArgs;
using Markerator.Helpers;

namespace Markerator;

public static class Markerator
{
    public static int Main(string[] args)
    {
        try
        {
            FluentArgsBuilder.New()
                .DefaultConfigsWithAppDescription(
                    "A very simple static website generator written in C#/.Net. " +
                    "Markerator converts Markdown files into a static HTML site.")
                .RegisterHelpFlag("-h", "--help")
                .Parameter<string>("-t", "--title")
                    .WithDescription(
                        "The title of the website. This is used as the site name in the " +
                        "navigation bar, the <title> of each generated page, and the " +
                        "og:site_name meta tag.")
                    .WithExamples("Markerator Generated Site", "zombo.com", "Bengineering")
                    .IsRequired()
                .Parameter<string>("-u", "--url")
                    .WithDescription(
                        "The base URL of the website, omitting the trailing slash. " +
                        "Used to build absolute URLs for Open Graph, Twitter Card, and RSS metadata.")
                    .WithExamples("https://www.slashdot.org", "https://elementary.io", "https://bengineeri.ng")
                    .IsRequired()
                .Parameter<string>("-i", "--indexFile")
                    .WithDescription(
                        "The markdown file that is to be converted into the index.html file. " +
                        "The path is relative to the /input folder.")
                    .WithExamples("index.md", "mainFile.md", "radicalText.md")
                    .IsRequired()
                .Parameter<bool>("-p", "--posts")
                    .WithDescription(
                        "Optional with default 'False'. Whether or not the site should " +
                        "include a posts link (like a news or updates section) in the navigation bar.")
                    .WithExamples("true", "false")
                    .IsOptionalWithDefault(false)
                .ListParameter<string>("-pt", "--postsTitle")
                    .WithDescription(
                        "Optional with default ''. A single title, or comma separated list of titles, " +
                        "that represents a link to each section of the site that will be a 'feed.' " +
                        "Each postsTitle specified should have a corresponding folder that contains " +
                        "one or more Markdown files. Multiple values can be used by joining them with " +
                        "any of the following separators: , ;")
                    .WithExamples("News", "Updates", "Blog", "Projects")
                    .IsOptionalWithDefault(new List<string>())
                .Parameter<bool>("-rss", "--rssFeed")
                    .WithDescription(
                        "Optional with default 'False'. Whether or not to generate RSS feeds from " +
                        "your posts/news/blog pages.")
                    .WithExamples("true", "false")
                    .IsOptionalWithDefault(false)
                .Parameter<bool>("-ri", "--rssIcon")
                    .WithDescription(
                        "Optional with default 'False'. If set to true, and an icon named 'rss.jpg' " +
                        "or 'rss.png' exists in the /images folder, then an icon link will be created " +
                        "that links to each page's RSS feed.")
                    .WithExamples("true", "false")
                    .IsOptionalWithDefault(false)
                .Parameter<bool>("-f", "--favicon")
                    .WithDescription(
                        "Optional with default 'False'. Whether or not the site should use a " +
                        "favicon.ico file in the /input/images directory.")
                    .WithExamples("true", "false")
                    .IsOptionalWithDefault(false)
                .ListParameter<string>("-op", "--otherPages")
                    .WithDescription(
                        "Optional with default ''. Additional pages that should be linked from the " +
                        "navigation bar, provided as a comma separated list of .md files. Multiple " +
                        "values can be used by joining them with any of the following separators: , ;")
                    .WithExamples("About.md", "Contact.md", "About.md,Contact.md")
                    .IsOptionalWithDefault(new List<string>())
                .Parameter<string>("-c", "--css")
                    .WithDescription(
                        "Optional with default ''. Include a custom CSS file that will theme the " +
                        "generated site. The file is resolved from the /input or /input/css folder. " +
                        "If omitted or not found, Markerator's built-in default stylesheet is used.")
                    .WithExamples("LightTheme.css", "DarkTheme.css", "site.css")
                    .IsOptionalWithDefault(string.Empty)
                .Parameter<int>("-pp", "--postsPerPage")
                    .WithDescription(
                        "Optional with default 0. This will tell Markerator how many posts should " +
                        "appear on the News/Updates/Blogs landing page. Specifying 0 will mean there " +
                        "is no limit, specifying any other number will create a series of pages that " +
                        "are linked to each other for pagination.")
                    .WithExamples(0, 5, 10, 25)
                    .IsOptionalWithDefault(0)
                .Call(
                    postsPerPage => css => otherPages => favicon => rssIcon => rssFeed =>
                    postsTitle => posts => indexFile => url => title =>
                {
                    Environment.ExitCode = Run(
                        title, url, indexFile, posts, postsTitle,
                        rssFeed, rssIcon, favicon, otherPages, css, postsPerPage);
                })
                .Parse(args);

            return Environment.ExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal: {ex.Message}");
            return 1;
        }
    }

    private static int Run(
        string title,
        string url,
        string indexFile,
        bool posts,
        IReadOnlyList<string> postsTitle,
        bool rssFeed,
        bool rssIcon,
        bool favicon,
        IReadOnlyList<string> otherPages,
        string css,
        int postsPerPage)
    {
        var normalizedTitles = NormalizeList(postsTitle);
        var normalizedPages = NormalizeList(otherPages);

        var inputDir = Path.Combine(Directory.GetCurrentDirectory(), "input");
        var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "output");

        if (!Directory.Exists(inputDir))
        {
            Console.Error.WriteLine($"Error: input directory not found at {inputDir}");
            return 1;
        }

        url = url.TrimEnd('/');

        if (posts && normalizedTitles.Count == 0)
        {
            normalizedTitles.Add("News");
        }

        Directory.CreateDirectory(outputDir);

        var generator = new HtmlGenerator(
            title: title,
            baseUrl: url,
            inputDir: inputDir,
            outputDir: outputDir,
            css: css,
            favicon: favicon,
            rssFeed: rssFeed,
            rssIcon: rssIcon,
            postsPerPage: postsPerPage,
            postsTitles: normalizedTitles,
            otherPages: normalizedPages);

        var result = generator.Generate(indexFile);

        if (result.IsFailed)
        {
            foreach (var err in result.Errors)
            {
                Console.Error.WriteLine($"Error: {err.Message}");
            }
            return 1;
        }

        Console.WriteLine("Site generated successfully.");
        return 0;
    }

    private static List<string> NormalizeList(IEnumerable<string> items)
    {
        if (items == null) return new List<string>();

        return items
            .SelectMany(s => (s ?? string.Empty).Split(
                new[] { ',', ';' },
                StringSplitOptions.RemoveEmptyEntries))
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }
}
