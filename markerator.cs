using System;
using System.IO;
using Markdig;
using FluentArgs;
using HtmlAgilityPack;
using System.Linq;

namespace com.github.benpocalypse
{
    class markerator
    {
        private static string defaultStyleHtml = @"
.topnav {
  overflow: hidden;
  position: fixed;
  top: 0px;
  margin-left: 0;
  padding-left: 40%;
  width: 100%;
  align-items: center;
  background-color: #333;
}

.topnav a {
  float: left;
  color: #f2f2f2;
  text-align: center;
  padding: 14px 16px;
  text-decoration: none;
  font-size: 17px;
}

.topnav a:hover {
  background-color: #ddd;
  color: black;
}

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
  font-size: 17px;
}

.content a:hover {
  background-color: #ddd;
  color: black;
}

head {
    margin-left: 0;
}

body {
    margin-left: 0;
    padding-top: 0;
}";

        static void Main(string[] args)
        {
            FluentArgsBuilder.New()
                .DefaultConfigsWithAppDescription("A static website generator written in C#.")
                .RegisterHelpFlag("-h", "--help")
                .Parameter<string>("-t", "--title")
                    .WithDescription("The title of the website.")
                    .WithExamples("Markerator Generated Site", "zombo.com")
                    .IsRequired()
                .Parameter<string>("-i", "--indexFile")
                    .WithDescription("The markdown file that is to be converted to the index.html file.")
                    .WithExamples("mainFile.md", "radicalText.md")
                    .IsRequired()
                .Parameter<bool>("-p", "--posts")
                    .WithDescription("Whether or not the site should include a posts link (like a news or updates section.)")
                    .WithExamples("true", "false")
                    .IsRequired()
                .Parameter<string>("-pt", "--postsTitle")
                    .WithDescription("The title that the posts section should use.")
                    .WithExamples("News", "Updates", "Blog")
                    .IsOptionalWithDefault("Posts")
                .Parameter<bool>("-f", "--favicon")
                    .WithDescription("Whether or not the site should use a favicon file in the images directory.")
                    .WithExamples("true", "false")
                    .IsOptionalWithDefault(false)
                .Call(favicon => postsTitle => posts => indexFile => siteTitle =>
                {
                    Console.WriteLine($"Creating site {siteTitle} with index of {indexFile}, including posts: {posts}...");

                    string contentFilename = Path.Combine(Directory.GetCurrentDirectory(), "input", indexFile);
                    string contentMarkdown = File.ReadAllText(contentFilename);
                    var contentPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                    string contentHtml = Markdown.ToHtml(contentMarkdown, contentPipeline);

                    string faviconHtml = @$"<link rel=""icon"" type=""image/x-icon"" href=""images/favicon.ico"">";
                    string navigationHtml = @$"<div class=""topnav"">
<a href=""./index.html"">{siteTitle}</a>
";

                // Create the output directory, and copy everything we need over.
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "output"));
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "output", "images"));
                Copy(
                    sourceDirectory: Path.Combine(Directory.GetCurrentDirectory(), "input", "images"),
                    targetDirectory: Path.Combine(Directory.GetCurrentDirectory(), "output", "images")
                );

                if (posts == true)
                {
                    navigationHtml += @$"<a href=""./posts.html"">{postsTitle}</a>
";
                    navigationHtml += "</div>";

                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "output", "posts"));

                    string postsIndexHtml = @$"<h1>{postsTitle}</h1>" + System.Environment.NewLine;

                    foreach (var postfile in Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "input", "posts")))
                    {
                        string postMarkdown = File.ReadAllText(postfile);
                        var postPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                        string postHtml = Markdown.ToHtml(postMarkdown, postPipeline);
                        var postHtmlFile = Path.GetFileNameWithoutExtension(postfile) + ".html";

                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(postHtml);

                        var postHtmlTitle = doc.DocumentNode.SelectNodes("//h1").FirstOrDefault().InnerText;
                        var postHtmlSummary = doc.DocumentNode.SelectNodes("//p").FirstOrDefault().InnerText;

                        Console.WriteLine("postHtmlFile = " + postHtmlFile);

                        postsIndexHtml += @$"<a href=""posts/{postHtmlFile}"">{postHtmlTitle}- {File.GetCreationTime(postfile)}
                        <p>{postHtmlSummary}</p>
                        </a>
                        <hr align=""left"">
";

                        string htmlPost = @$"<!DOCTYPE html>
<html>
    <head>
        <style>
            {defaultStyleHtml}
        </style>
        <title>{siteTitle}</title>
        {faviconHtml}
        {navigationHtml}
    </head>
    <body>
        <div class=""content"">
            {postHtml}
        </div>
    </body>
</html> ";

                        File.WriteAllText(
                            Path.Combine(
                                Directory.GetCurrentDirectory(),
                                "output",
                                "posts",
                                postHtmlFile
                                ),
                            htmlPost);
                    }

                    string htmlPosts = @$"<!DOCTYPE html>
<html>
    <head>
        <style>
            {defaultStyleHtml}
        </style>
        <title>{siteTitle}</title>
        {faviconHtml}
        {navigationHtml}
    </head>
    <body>
        <div class=""content"">
            {postsIndexHtml}
        </div>
    </body>
</html> ";

                    File.WriteAllText(
                        Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "output",
                            "posts.html"
                            ),
                        htmlPosts);
                }
                else
                {
                    navigationHtml += "</div>";
                }

                string htmlIndex = @$"<!DOCTYPE html>
<html>
    <head>
        <style>
            {defaultStyleHtml}
        </style>
        <title>{siteTitle}</title>
        {faviconHtml}
        {navigationHtml}
    </head>
    <body>
        <div class=""content"">
            {contentHtml}
        </div>
    </body>
</html> ";

                File.WriteAllText(
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "output",
                        "index.html"
                        ),
                    htmlIndex);

                Console.WriteLine($"...site generation successful.");

                })
                .Parse(args);
        }

        private static void Copy(string sourceDirectory, string targetDirectory)
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
    }
}
