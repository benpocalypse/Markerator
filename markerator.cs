using System;
using System.IO;
using Markdig;
using FluentArgs;
using FluentResults;
using HtmlAgilityPack;
using System.Linq;
using System.Collections.Generic;
using ExCSS;

namespace com.github.benpocalypse
{
    public class markerator
    {
        private static string _version = "0.2.2";
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
                .Parameter<string>("-c", "--css")
                    .WithDescription("Inlude a custom CSS file that will theme the generated site.")
                    .WithExamples("LightTheme.css", "DarkTheme.css")
                    .IsOptionalWithDefault("")
                .Call(customCss => otherPages => favicon => postsTitle => posts => indexFile => siteTitle =>
                {
                    Console.WriteLine($"Creating site {siteTitle} with index of {indexFile}, including posts: {posts}...");

                    CreateOutputDirectories();

                    var css = ValidateAndGetCustomCssContents (customCss);

                    css.IsFailed.IfTrue(() =>
                    {
                        Console.WriteLine("Failed to parse custom css, using default instead.");
                        css = Result.Ok(defaultCss);
                    });

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

                    posts.IfTrue(() =>
                    {
                        CreateHtmlPostPages(
                            includeFavicon: favicon,
                            postsTitle: postsTitle,
                            siteTitle: siteTitle,
                            otherPages: otherPages,
                            css: css.Value
                        );
                    });

                    Console.WriteLine($"...site generation successful.");
                })
                .Parse(args);
        }

        private static Result<string> ValidateAndGetCustomCssContents(string cssFilenames)
        {
            return Result.Try<string>(() =>
            {
                if (cssFilenames.Equals(string.Empty))
                {
                    return defaultCss;
                }

                var parser = new StylesheetParser();
                string cssFilePath = Path.Combine(Directory.GetCurrentDirectory(), "input", cssFilenames);
                string cssContent = File.ReadAllText(cssFilePath);
                var stylesheet = parser.Parse(cssContent);

                foreach (var rule in stylesheet.StyleRules)
                {
                    if (!rule.SelectorText.Contains(".navigation-title") &&
                        !rule.SelectorText.Contains(".navigation") &&
                        !rule.SelectorText.Contains(".content") &&
                        !rule.SelectorText.Contains("head") &&
                        !rule.SelectorText.Contains("body") &&
                        !rule.SelectorText.Contains("h"))
                        {
                            Console.WriteLine("Returning default Css.");
                            throw new Exception($"Failed to parse {cssFilenames}.");
                        }
                }

                Console.WriteLine("Returning custom Css.");
                return cssContent;
            });
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

        private static void CreateHtmlPostPages(
            bool includeFavicon,
            string postsTitle,
            string siteTitle,
            IReadOnlyList<string> otherPages,
            string css)
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "output", "posts"));

            string postsIndexHtml = @$"<h2>{postsTitle}</h2>" + System.Environment.NewLine;

            var postFiles = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "input", "posts")).OrderByDescending(f => File.GetCreationTime(f)).ToArray();

            string currentYear = "0000";

            foreach (var postfile in postFiles)
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

                if (!currentYear.Equals(File.GetCreationTime(postfile).ToString("yyyy")))
                {
                    currentYear = File.GetCreationTime(postfile).ToString("yyyy");
                    postsIndexHtml += @$"<h3>{currentYear}</h3>
                    ";
                }

                // TODO - Maybe? support images/cards for post summaries, or perhaps some sort of custom formatting?
                //      - Or allow some CLI options to show summaries under links, etc?

                postsIndexHtml += @$"&emsp;<a href=""posts/{postHtmlFile}"">{File.GetCreationTime(postfile).ToString("MM/dd")} - {postHtmlTitle}</a><br/>
&emsp;{postHtmlSummary}
<br/>
<br/>
";
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
        {(includeFavicon == true ?
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
        <p>Site generated with <a href=""https://github.com/benpocalypse/Markerator"">Markerator</a>.</p>
    </footer>
";

        private static string defaultCss = @"
.navigation-title {
    overflow: hidden;
    position: fixed;
    top: 0px;
    margin-left: 0;
    padding-left: 40%;
    width: 100%;
    align-items: center;
    background-color: #fcf7f0;
}

.navigation-title a {
    float: left;
    color: #2e2e2e;
    text-align: center;
    padding: 10px 16px;
    text-decoration: none;
    font-size: 22px;
}

.navigation {
    overflow: hidden;
    position: fixed;
    top: 35px;
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
    padding: 10px 16px;
    text-decoration: none;
    font-size: 16px;
}

.navigation a:hover {
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

.dropdown-content a:hover {
    background-color: #ddd;
}

.dropdown:hover .dropdown-content {
    display: block;
}

.dropdown:hover .dropbtn {
    background-color: #3e8e41;
}

.content {
    padding-left: 20%;
    padding-right: 20%;
    padding-top: 40px;
    padding-bottom: 100px;
}

.content a {
    color: #515151;
    text-align: left;
    text-decoration: none;
}

.content a:hover {
    color: black;
}

head {
    margin-left: 0;
}

h1 {
    color: #5e5e5e;
}

h2 {
    color: #5e5e5e;
}

h3 {
    color: #5e5e5e;
}

h4 {
    color: #5e5e5e;
}

h5 {
    color: #5e5e5e;
}

h6 {
    color: #5e5e5e;
}

body {
    background-color: #fcf7f0;
    color: #5e5e5e;
    margin-left: 0;
    padding-top: 0;
}

footer {
    text-align: center;
    padding: 6px;
    background-color: #fcf7f0;
    color: #5e5e5e;
    font-size: 12px;
}";
    }
}
