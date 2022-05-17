﻿using System;
using System.IO;
using Markdig;
using FluentArgs;
using HtmlAgilityPack;
using System.Linq;
using System.Collections.Generic;

namespace com.github.benpocalypse
{
    public class markerator
    {
        private static string _version = "0.1.0";
        static void Main(string[] args)
        {
            FluentArgsBuilder.New()
                .DefaultConfigsWithAppDescription(@$"Markerator v{_version}.
A very simple static website generator written in C#.")
                .RegisterHelpFlag("-h", "--help")
                .Parameter<string>("-t", "--title")
                    .WithDescription("The title of the website.")
                    .WithExamples("Markerator Generated Site", "zombo.com")
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
                .Parameter<bool>("-f", "--favicon")
                    .WithDescription("Whether or not the site should use a favicon.ico file in the /input/images directory.")
                    .WithExamples("true", "false")
                    .IsOptionalWithDefault(false)
                .ListParameter<string>("-op", "--otherPages")
                    .WithDescription("Additional pages that should be linked from the navigation bar, provided as a list of .md files.")
                    .WithExamples("About.md,Contact.md")
                    .IsOptionalWithDefault(new List<string>())
                .ListParameter<string>("-c", "--css")
                    .WithDescription("Inlude custom CSS file that will theme the generated site.")
                    .WithExamples("LightTheme.css", "DarkTheme.css")
                    .IsOptionalWithDefault(new List<string>())
                .Call(customCss => otherPages => favicon => postsTitle => posts => indexFile => siteTitle =>
                {
                    Console.WriteLine($"Creating site {siteTitle} with index of {indexFile}, including posts: {posts}...");

                    CreateOutputDirectories();

                    // TODO - Impelement Css file handling
                    // CreateCssSection (customCss) ;

                    Console.WriteLine(
                        CreateHtmlPage(
                            otherPages: otherPages,
                            markdownFile: indexFile,
                            includeFavicon: favicon,
                            includePosts: posts,
                            postsTitle: postsTitle,
                            siteTitle: siteTitle,
                            css: customCss.Count() == 0 ? new List<string>(){defaultCss} : customCss,
                            isIndex: true)
                        );

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
                                    css: customCss.Count() == 0 ? new List<string>(){defaultCss} : customCss,
                                    isIndex: false)
                                );
                        }
                    });

                    posts.IfTrue(() =>
                    {
                        CreateHtmlPostPages(
                            includeFavicon: favicon,
                            postsTitle: postsTitle,
                            siteTitle: siteTitle,
                            otherPages: otherPages,
                            css: customCss.Count() == 0 ? new List<string>(){defaultCss} : customCss
                        );
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
            IReadOnlyList<string> css,
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

        private static void CreateHtmlPostPages(
            bool includeFavicon,
            string postsTitle,
            string siteTitle,
            IReadOnlyList<string> otherPages,
            IReadOnlyList<string> css)
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "output", "posts"));

            string postsIndexHtml = @$"<h1>{postsTitle}</h1>" + System.Environment.NewLine;

            foreach (var postfile in Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "input", "posts")))
            {
                string postMarkdown = File.ReadAllText(postfile);
                var postPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                string postHtml = Markdown.ToHtml(postMarkdown, postPipeline);
                var postHtmlFile = Path.GetFileNameWithoutExtension(postfile) + ".html";

                Console.WriteLine($"...adding post for {postfile}...");

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(postHtml);

                var postHtmlTitle = doc.DocumentNode.SelectNodes("//h1").FirstOrDefault().InnerText;
                var postHtmlSummary = doc.DocumentNode.SelectNodes("//p").FirstOrDefault().InnerText;

                // TODO - Support images/cards for post summaries.

                postsIndexHtml += @$"<a href=""posts/{postHtmlFile}"">{postHtmlTitle} - {File.GetCreationTime(postfile)}
                <p>{postHtmlSummary}</p>
                </a>
                <hr align=""left"">
";

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
                        "posts",
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
                    "posts.html"
                    ),
                htmlPosts);
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
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

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
            IReadOnlyList<string> css,
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
            {GetFontCss(isPosts: false)}
            {css}
        </style>
        <title>{siteTitle}</title>
        {(includeFavicon == true ?
@$"     <link rel=""icon"" type=""image/x-icon"" href=""images/favicon.ico"">" : string.Empty)}
        {GetNavigationHtml(
            siteTitle: siteTitle,
            css: css,
            includePosts: includePosts,
            postsTitle: postsTitle,
            otherPages: otherPages,
            isPosts: isPosts)}
        {GetThemeMenuHtml(css)}
    </head>
    <body>
        <div class=""content"">
            {html}
        </div>
    </body>
</html> ";

        private static string GetNavigationHtml(
            IReadOnlyList<string> otherPages,
            IReadOnlyList<string> css,
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
@$"<div class=""navigation"">
            <a href=""{(isPosts ==  true ? ".." : ".")}/index.html"">{siteTitle}</a>
    {(includePosts == true ?
@$"         <a href=""{(isPosts ==  true ? ".." : ".")}/posts.html"">{postsTitle}</a>
            {otherPagesHtml}
            {GetThemeMenuHtml(css)}
        </div>
" : @$"
        {otherPagesHtml}
        {GetThemeMenuHtml(css)}
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

        private static string GetThemeMenuHtml(IReadOnlyList<string> css)
        {
            string resultHtml =string.Empty;

            if (css.Count > 0)
            {
                resultHtml = @$"
        <div class=""dropdown"">
            <button class=""dropdownbutton"">Theme</button>
            <div class=""dropdown-content"">
";

                foreach (var theme in css)
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


        private static string defaultCss =
@".navigation {
    overflow: hidden;
    position: fixed;
    top: 0px;
    margin-left: 0;
    padding-left: 40%;
    width: 100%;
    align-items: center;
    background-color: #fcf7f0;
}

.navigation a {
    float: left;
    color: #5e5e5e;
    text-align: center;
    padding: 14px 16px;
    text-decoration: none;
    font-size: 16px;
}

.navigation a:hover {
    background-color: #ddd;
    color: black;
}

.dropdownbutton {
    background-color: #333;
    color: #f2f2f2;
    font-size: 16px;
    padding: 6px;
    padding-right: 40px;
    border: none;
}

.dropdown {
    position: relative;
    display: inline-block;
    float: right;
}

.dropdown-content {
    display: none;
    position: absolute;
    background-color: #f1f1f1;
    min-width: 160px;
    box-shadow: 0px 8px 16px 0px rgba(0,0,0,0.2);
    z-index: 1;
}

.dropdown-content a {
    color: black;
    padding: 12px 16px;
    text-decoration: none;
    display: block;
}

.dropdown-content a:hover { background-color: #ddd; }

.dropdown:hover .dropdown-content { display: block; }

.dropdown:hover .dropbtn { background-color: #3e8e41; }

.content {
    padding-left: 20%;
    padding-right: 20%;
    padding-top: 30px;
    padding-bottom: 100px;
}

.content a {
    color: #515151;
    text-align: left;
    text-decoration: none;
    font-size: 12px;
}

.content a:hover {
    background-color: #ddd;
    color: black;
}

head {
    margin-left: 0;
}

h1 {
    color: #333333;
}

h2 {
    color: #333333;
}

h3 {
    color: #333333;
}

h4 {
    color: #333333;
}

h5 {
    color: #333333;
}

h6 {
    color: #333333;
}

body {
    background-color: #fcf7f0;
    color: #5e5e5e;
    margin-left: 0;
    padding-top: 0;
}";
    }
}
