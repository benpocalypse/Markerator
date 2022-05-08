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
        static void Main(string[] args)
        {
            FluentArgsBuilder.New()
                .DefaultConfigsWithAppDescription("A very simple static website generator written in C#.")
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
                .Parameter<string>("-c", "--css")
                    .WithDescription("Inlude custom CSS file that will theme the generated site.")
                    .WithExamples("LightTheme.css", "DarkTheme.css")
                    .IsOptionalWithDefault(defaultCss)
                .Call(customCss => favicon => postsTitle => posts => indexFile => siteTitle =>
                {
                    Console.WriteLine($"Creating site {siteTitle} with index of {indexFile}, including posts: {posts}...");

                    CreateOutputDirectories();

                    // TODO - Impelement Css file handling
                    // CreateCssSection (customCss) ;

                    CreateHtmlPage(
                        markdownFile: indexFile,
                        includeFavicon: favicon,
                        includePosts: posts,
                        postsTitle: postsTitle,
                        siteTitle: siteTitle,
                        css: customCss
                    );

                    posts.IfTrue(() =>
                    {
                        CreateHtmlPostPages(
                            includeFavicon: favicon,
                            postsTitle: postsTitle,
                            siteTitle: siteTitle,
                            css: customCss
                        );
                    });

                    Console.WriteLine($"...site generation successful.");
                })
                .Parse(args);
        }

        private static void CreateHtmlPage(string markdownFile, bool includeFavicon, bool includePosts, string postsTitle, string siteTitle, string css)
        {
            string contentFilename = Path.Combine(Directory.GetCurrentDirectory(), "input", markdownFile);
            string contentMarkdown = File.ReadAllText(contentFilename);
            var contentPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            string contentHtml = Markdown.ToHtml(contentMarkdown, contentPipeline);
            var markdownHtmlFile = Path.GetFileNameWithoutExtension(markdownFile) + ".html";

            var htmlIndex = GetPageHtml(
                siteTitle: siteTitle,
                html: contentHtml,
                includeFavicon: includeFavicon,
                includePosts: includePosts,
                postsTitle: postsTitle,
                css: css
            );

            File.WriteAllText(
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "output",
                    markdownHtmlFile
                    ),
                    htmlIndex);
        }

        private static void CreateHtmlPostPages(bool includeFavicon, string postsTitle, string siteTitle, string css)
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "output", "posts"));

            string postsIndexHtml = @$"<h1>{postsTitle}</h1>" + System.Environment.NewLine;

            foreach (var postfile in Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "input", "posts")))
            {
                string postMarkdown = File.ReadAllText(postfile);
                var postPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                string postHtml = Markdown.ToHtml(postMarkdown, postPipeline);
                var postHtmlFile = Path.GetFileNameWithoutExtension(postfile) + ".html";

                Console.WriteLine($"..adding post for {postfile}...");

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(postHtml);

                var postHtmlTitle = doc.DocumentNode.SelectNodes("//h1").FirstOrDefault().InnerText;
                var postHtmlSummary = doc.DocumentNode.SelectNodes("//p").FirstOrDefault().InnerText;

                // TODO - Support images/cards for post summaries.

                postsIndexHtml += @$"<a href=""posts/{postHtmlFile}"">{postHtmlTitle}- {File.GetCreationTime(postfile)}
                <p>{postHtmlSummary}</p>
                </a>
                <hr align=""left"">
";

                var htmlPost = GetPostHtml(
                    siteTitle: siteTitle,
                    postsTitle: postsTitle,
                    html: postHtml,
                    includeFavicon: includeFavicon,
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
                siteTitle: siteTitle,
                html: postsIndexHtml,
                includeFavicon: includeFavicon,
                includePosts: true,
                postsTitle: postsTitle,
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
            string siteTitle = "",
            string html = "",
            bool includeFavicon = false,
            bool includePosts = false,
            string postsTitle = "",
            string css = defaultCss
            ) =>
@$"<!DOCTYPE html>
<html>
    <head>
        <style>
            {(css == string.Empty ?
                defaultCss :
                css)}
        </style>
        <title>{siteTitle}</title>
        {(includeFavicon == true ?
@$"     <link rel=""icon"" type=""image/x-icon"" href=""images/favicon.ico"">" : string.Empty)}
        {(
@$"     <div class=""navigation"">
            <a href=""./index.html"">{siteTitle}</a>
        {(includePosts == true ?
@$"         <a href=""./posts.html"">{postsTitle}</a>
            {themeHtml}
        </div>
" : @$"
        {themeHtml}
        </div>")}
")}
        {themeHtml}
    </head>
    <body>
        <div class=""content"">
            {html}
        </div>
    </body>
</html> ";

        private static string GetPostHtml(
            string siteTitle = "",
            string postsTitle = "",
            string html = "",
            bool includeFavicon = false,
            string css = defaultCss
            ) =>
@$"<!DOCTYPE html>
<html>
    <head>
        <style>
            {css}
        </style>
        <title>{siteTitle}</title>
        {(includeFavicon == true ?
@$"     <link rel=""icon"" type=""image/x-icon"" href=""images/favicon.ico"">" : string.Empty)}
        <div class=""navigation"">
            <a href=""../index.html"">{siteTitle}</a>
            <a href=""../posts.html"">{postsTitle}</a>
        </div>
        {themeHtml}
    </head>
    <body>
        <div class=""content"">
            {html}
        </div>
    </body>
</html> ";

        private const string themeHtml = @"
        <div class=""dropdown"">
            <button class=""dropdownbutton"">Theme</button>
            <div class=""dropdown-content"">
                <a href=""#"">Link 1</a>
                <a href=""#"">Link 2</a>
                <a href=""#"">Link 3</a>
            </div>
        </div>
        ";

        private const string defaultCss = @"
.navigation {
  overflow: hidden;
  position: fixed;
  top: 0px;
  margin-left: 0;
  padding-left: 40%;
  width: 100%;
  align-items: center;
  background-color: #333;
}

.navigation a {
  float: left;
  color: #f2f2f2;
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

.dropdown-content a:hover {background-color: #ddd;}

.dropdown:hover .dropdown-content {display: block;}

.dropdown:hover .dropbtn {background-color: #3e8e41;}

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
    }
}
