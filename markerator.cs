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
using markerator.Helpers;

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
            .WithDescription(
                "A single title, or comma separated list of titles, that represents a link to each section of the site that will be a 'feed.' Each postsTitle specified should have a corresponding folder that contains one or more Markdown files.")
            .WithExamples("News", "Updates", "Blog, Projects")
            .IsOptionalWithDefault(default(List<string>)!)
            .Parameter<bool>("-rss", "--rssFeed")
            .WithDescription("Whether or not to generate Rss feeds from your posts/news/blog pages.")
            .WithExamples("true", "false")
            .IsOptionalWithDefault(false)
            .Parameter<bool>("-ri", "--rssIcon")
            .WithDescription(
                "If set to true, and an icon named 'rss.jpg' or 'rss.png' exists in the /images folder, then an icon link will be created that links to each pages Rss feed.")
            .WithExamples("true", "false")
            .IsOptionalWithDefault(false)
            .Parameter<bool>("-f", "--favicon")
            .WithDescription("Whether or not the site should use a favicon.ico file in the /input/images directory.")
            .WithExamples("true", "false")
            .IsOptionalWithDefault(false)
            .ListParameter<string>("-op", "--otherPages")
            .WithDescription(
                "Additional pages that should be linked from the navigation bar, provided as a comma separated list of .md files.")
            .WithExamples("About.md,Contact.md")
            .IsOptionalWithDefault(default(List<string>)!)
            .Parameter<string>("-c", "--css")
            .WithDescription("Inlude a custom CSS file that will theme the generated site.")
            .WithExamples("LightTheme.css", "DarkTheme.css")
            .IsOptionalWithDefault("")
            .Call(customCss =>
                otherPages =>
                favicon =>
                rssIcon =>
                rss =>
                postsTitle =>
                posts =>
                indexFile =>
                baseUrl =>
                siteTitle =>
                {
                    var result = $"...site generation successful.";
                    var success = true;

                    Console.WriteLine(
                        $"Creating site {siteTitle} with index of {indexFile}, including posts: {posts}...");

                    // FIXME - Figure out how to do this without blowing out the .git folder
                    //DeleteOutputDirectorsIfExists();
                    DirectoryUtils.CreateOutputDirectories();

                    var css = CssValidator.ValidateAndGetCustomCssContents(customCss);

                    css.IsFailed.IfTrue(() =>
                    {
                        Console.WriteLine(
                            "Failed to parse custom css, using default css instead.");
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
                            baseUrl: baseUrl.ToString(),
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
                                    baseUrl: baseUrl.ToString(),
                                    isIndex: false)
                            );
                        }
                    });

                    var rssImageFilename = rssIcon == true
                        ? RssGenerator.GetRssImageFilename()
                        : string.Empty;

                    // ...and if there are any "news/posts/projects" pages, add those as well.
                    posts.IfTrue(() =>
                    {
                        if (rss == true && rssIcon == false)
                        {
                            success = false;
                            result =
                                $"...site generation failed. If Rss generation is true, an rssIcon must be specified.";
                        }
                        else
                        {
                            if (rss == false && rssIcon == true)
                            {
                                success = false;
                                result =
                                    $"...site generation failed. An rssIcon should not be included if Rss generation isn't true.";
                            }
                            else
                            {
                                RssGenerator.VerifyRssImageExistsInOutput()
                                    .IfFalse(() =>
                                    {
                                        success = false;
                                        result =
                                            $"...site generation failed. An rssIcon was not found. Please ensure you have a file named either 'rss.png' or 'rss.jpg' in your input/images folder.";
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
                                baseUrl: baseUrl.ToString(),
                                rss: rss,
                                rssImage: rssImageFilename,
                                css: css.Value
                            );


                            if (success == true && rss == true && rssIcon == true)
                            {
                                // TODO - this will need to account for multiple posts/news/blogs/projects in the future.
                                RssGenerator.GenerateRssFeed(post, siteTitle, baseUrl,
                                    postCollection);
                            }
                        }
                    });

                    Console.WriteLine(result);
                    //success.IfFalse(() => DeleteOutputDirectorsIfExists());
                }).Parse(args);
    }
}
