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
                .DefaultConfigsWithAppDescription("A very simple static website generator written in C#.")
                .RegisterHelpFlag("-h", "--help")
                .Parameter<string>("-t", "--title")
                    .WithDescription("The title of the website.")
                    .IsRequired()
                .Parameter<string>("-u", "--url")
                    .WithDescription("The base URL of the website, omitting the trailing slash.")
                    .IsRequired()
                .Parameter<string>("-i", "--indexFile")
                    .WithDescription("The markdown file to convert into index.html.")
                    .IsRequired()
                .Parameter<bool>("-p", "--posts")
                    .WithDescription("Optional with default 'False'. Whether to include Posts/News sections.")
                    .IsOptionalWithDefault(false)
                .ListParameter<string>("-pt", "--postsTitle")
                    .WithDescription("Optional. Section titles (e.g. News,Updates,Blog).")
                    .IsOptionalWithDefault(new List<string>())
                .Parameter<bool>("-rss", "--rssFeed")
                    .WithDescription("Optional with default 'False'. Whether to generate RSS feeds.")
                    .IsOptionalWithDefault(false)
                .Parameter<bool>("-ri", "--rssIcon")
                    .WithDescription("Optional with default 'False'. Whether to show an RSS icon linking to the feed.")
                    .IsOptionalWithDefault(false)
                .Parameter<bool>("-f", "--favicon")
                    .WithDescription("Optional with default 'False'. Whether to include a favicon from input/images.")
                    .IsOptionalWithDefault(false)
                .ListParameter<string>("-op", "--otherPages")
                    .WithDescription("Optional. Extra .md pages for the nav bar.")
                    .IsOptionalWithDefault(new List<string>())
                .Parameter<string>("-c", "--css")
                    .WithDescription("Optional. Custom CSS file to theme the site.")
                    .IsOptionalWithDefault(string.Empty)
                .Parameter<int>("-pp", "--postsPerPage")
                    .WithDescription("Optional with default '0'. Posts per page. 0 = no pagination.")
                    .IsOptionalWithDefault(0)
                .Call(
                    postsPerPage => css => otherPages => favicon => rssIcon => rssFeed =>
                    postsTitle => posts => indexFile => url => title =>
                {
                    Environment.ExitCode = Run(
                        title, url, indexFile, posts, postsTitle.ToList(),
                        rssFeed, rssIcon, favicon, otherPages.ToList(), css, postsPerPage);
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
        List<string> postsTitle,
        bool rssFeed,
        bool rssIcon,
        bool favicon,
        List<string> otherPages,
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

    private static List<string> NormalizeList(IEnumerable<string>? items)
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
