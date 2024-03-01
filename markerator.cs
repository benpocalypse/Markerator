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
    static void Main(string[] args)
    {
        FluentArgsBuilder.New()
            .DefaultConfigsWithAppDescription(@$"Markerator v{Globals.Version}.
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
            .ListParameter<string>("-pt", "--postsTitle")
                .WithDescription("A single title, or comma separated list of titles, that represents a link to each section of the site that will be a 'feed.' Each postsTitle specified should have a corresponding folder that contains one or more Markdown files.")
                .WithExamples("News", "Updates", "Blog, Projects")
                .IsOptionalWithDefault(default(List<string>))
            .Parameter<bool>("-rss", "--rssFeed")
                .WithDescription("Whether or not to generate Rss feeds from your posts/news/blog pages.")
                .WithExamples("true", "false")
                .IsOptionalWithDefault(false)
            .Parameter<bool>("-ri", "--rssIcon")
                .WithDescription("If set to true, and an icon named 'rss.jpg' or 'rss.png' exists in the /images folder, then an icon link will be created that links to each pages Rss feed.")
                .WithExamples("true", "false")
                .IsOptionalWithDefault(false)
            .Parameter<bool>("-f", "--favicon")
                .WithDescription("Whether or not the site should use a favicon.ico file in the /input/images directory.")
                .WithExamples("true", "false")
                .IsOptionalWithDefault(false)
            .ListParameter<string>("-op", "--otherPages")
                .WithDescription("Additional pages that should be linked from the navigation bar, provided as a comma separated list of .md files.")
                .WithExamples("About.md,Contact.md")
                .IsOptionalWithDefault(default(List<string>))
            .Parameter<string>("-c", "--css")
                .WithDescription("Inlude a custom CSS file that will theme the generated site.")
                .WithExamples("LightTheme.css", "DarkTheme.css")
                .IsOptionalWithDefault("")
            .Call(customCss => otherPages => favicon => rssIcon => rss => postsTitle => posts => indexFile => baseUrl => siteTitle =>
            {
                var result = $"...site generation successful.";
                var success = true;

                Console.WriteLine($"Creating site {siteTitle} with index of {indexFile}, including posts: {posts}...");

                //DeleteOutputDirectorsIfExists();
                CreateOutputDirectories();

                var css = CssValidator.ValidateAndGetCustomCssContents(customCss);

                css.IsFailed.IfTrue(() =>
                {
                    Console.WriteLine("Failed to parse custom css, using default css instead.");
                    css = Result.Ok(Globals.DefaultCss);
                });

                // Create index.html
                Console.WriteLine(
                    HtmlGenerator.CreateHtmlPage(
                        otherPages: otherPages,
                        markdownFile: indexFile,
                        includeFavicon: favicon,
                        includePosts: posts,
                        postsTitle: postsTitle.ToList(),
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
                            HtmlGenerator.CreateHtmlPage(
                                otherPages: otherPages,
                                markdownFile: page,
                                includeFavicon: favicon,
                                includePosts: posts,
                                postsTitle: postsTitle.ToList(),
                                siteTitle: siteTitle,
                                css: css.Value,
                                isIndex: false)
                            );
                    }
                });

                var rssImageFilename = RssGenerator.GetRssImageFilename();

                // ...and if there are any "news/posts/projects" pages, add those as well.
                posts.IfTrue(() =>
                {
                    if (rss == true && rssIcon == false)
                    {
                        success = false;
                        result = $"...site generation failed. If Rss generation is true, and rssIcon must be specified.";
                    }
                    else
                    {
                        if (rss == false && rssIcon == true)
                        {
                            success = false;
                            result = $"...site generation failed. An rssIcon should not be included if Rss generation isn't true.";
                        }
                        else
                        {
                            RssGenerator.VerifyRssImageExistsInOutput().IfFalse(() =>
                            {
                                success = false;
                                result = $"...site generation failed. An rssIcon was not found. Please ensure you have a file named either 'rss.png' or 'rss.jpg' in your input/images folder.";
                            });
                        }
                    }

                    foreach (var post in postsTitle)
                    {
                        var postCollection = HtmlGenerator.CreateHtmlPostPages(
                            includeFavicon: favicon,
                            postsTitle: post,
                            siteTitle: siteTitle,
                            otherPages: otherPages,
                            rss: rss,
                            rssImage: rssImageFilename,
                            css: css.Value
                        );


                        if (success == true && rss == true && rssIcon == true)
                        {
                            // TODO - this will need to account for multiple posts/news/blogs/projects in the future.
                            RssGenerator.GenerateRssFeed(post, siteTitle, baseUrl, postCollection);
                        }
                    }
                });

                Console.WriteLine(result);
                //success.IfFalse(() => DeleteOutputDirectorsIfExists());
            })
            .Parse(args);
    }

    private static void DeleteOutputDirectorsIfExists()
    {
        if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "output")))
        {
            Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), "output"), true);
        }
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
}
