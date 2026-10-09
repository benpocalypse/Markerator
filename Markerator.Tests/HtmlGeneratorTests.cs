using FluentAssertions;
using Xunit;
using Markerator.Helpers;

namespace Markerator.Tests;

public class HtmlGeneratorTests : IDisposable
{
    private readonly string _root;
    private readonly string _inputDir;
    private readonly string _outputDir;

    public HtmlGeneratorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "markerator-tests-" + Guid.NewGuid().ToString("N"));
        _inputDir = Path.Combine(_root, "input");
        _outputDir = Path.Combine(_root, "output");
        Directory.CreateDirectory(_inputDir);
        Directory.CreateDirectory(_outputDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    // ------------------------------------------------------------------
    // Fixtures
    // ------------------------------------------------------------------

    private void WriteIndex(string content = "# Welcome\n\nHello world.")
    {
        File.WriteAllText(Path.Combine(_inputDir, "index.md"), content);
    }

    private void WritePost(string section, string name, string content)
    {
        var dir = Path.Combine(_inputDir, section);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, name + ".md"), content);
    }
    
    [Fact]
    public void Generate_WritesStylesheetToOutputCssDirectory()
    {
        WriteIndex();
        Directory.CreateDirectory(Path.Combine(_inputDir, "css"));
        File.WriteAllText(Path.Combine(_inputDir, "css", "site.css"), "body { color: red; }");

        Build().Generate("index.md");

        var cssOut = Path.Combine(_outputDir, "css", "site.css");
        File.Exists(cssOut).Should().BeTrue();
        File.ReadAllText(cssOut).Should().Contain("color: red");
    }

    [Fact]
    public void Generate_IndexHtml_ReferencesStylesheet()
    {
        WriteIndex();
        Directory.CreateDirectory(Path.Combine(_inputDir, "css"));
        File.WriteAllText(Path.Combine(_inputDir, "css", "site.css"), "body { color: red; }");

        Build().Generate("index.md");

        var html = File.ReadAllText(Path.Combine(_outputDir, "index.html"));
        html.Should().Contain("<link rel=\"stylesheet\" href=\"/css/site.css\" />");
    }

    private HtmlGenerator Build(
        string title = "Test Site",
        string baseUrl = "https://example.com",
        int postsPerPage = 0,
        List<string> postsTitles = null!,
        bool rssFeed = false,
        bool rssIcon = false,
        bool favicon = false,
        List<string> otherPages = null!,
        string css = "")
    {
        return new HtmlGenerator(
            title: title,
            baseUrl: baseUrl,
            inputDir: _inputDir,
            outputDir: _outputDir,
            css: css,
            favicon: favicon,
            rssFeed: rssFeed,
            rssIcon: rssIcon,
            postsPerPage: postsPerPage,
            postsTitles: postsTitles ?? new List<string>(),
            otherPages: otherPages ?? new List<string>());
    }

    // ------------------------------------------------------------------
    // Index page
    // ------------------------------------------------------------------

    [Fact]
    public void Generate_ProducesIndexHtml()
    {
        WriteIndex();

        var result = Build().Generate("index.md");

        result.IsSuccess.Should().BeTrue();
        File.Exists(Path.Combine(_outputDir, "index.html")).Should().BeTrue();
    }

    [Fact]
    public void Generate_IndexHtml_ContainsOpenGraphTags()
    {
        WriteIndex("# Welcome\n\nHello world, this is the description.");

        Build().Generate("index.md");

        var html = File.ReadAllText(Path.Combine(_outputDir, "index.html"));
        html.Should().Contain("og:title")
                    .And.Contain("og:description")
                    .And.Contain("og:url")
                    .And.Contain("og:site_name")
                    .And.Contain("https://example.com/");
    }

    [Fact]
    public void Generate_FailsWhenIndexFileMissing()
    {
        var result = Build().Generate("index.md");

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
              .Which.Message.Should().ContainEquivalentOf("not found");
    }

    [Fact]
    public void Generate_FailsWhenInputDirectoryMissing()
    {
        Directory.Delete(_inputDir, recursive: true);

        var result = Build().Generate("index.md");

        result.IsFailed.Should().BeTrue();
    }

    // ------------------------------------------------------------------
    // Posts section: no pagination
    // ------------------------------------------------------------------

    [Fact]
    public void Generate_WithoutPagination_PutsAllPostsOnSectionPage()
    {
        WriteIndex();
        WritePost("News", "post1", "# 2024-01-01 First\n\nBody one.");
        WritePost("News", "post2", "# 2024-01-02 Second\n\nBody two.");
        WritePost("News", "post3", "# 2024-01-03 Third\n\nBody three.");

        var result = Build(postsTitles: new List<string> { "News" }).Generate("index.md");

        result.IsSuccess.Should().BeTrue();
        File.Exists(Path.Combine(_outputDir, "News.html")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDir, "News-2.html")).Should().BeFalse();

        var newsHtml = File.ReadAllText(Path.Combine(_outputDir, "News.html"));
        newsHtml.Should().Contain("First")
                       .And.Contain("Second")
                       .And.Contain("Third");
    }

    [Fact]
    public void Generate_WithoutPagination_GeneratesIndividualPostPages()
    {
        WriteIndex();
        WritePost("News", "post1", "# 2024-01-01 First\n\nBody one.");

        Build(postsTitles: new List<string> { "News" }).Generate("index.md");

        File.Exists(Path.Combine(_outputDir, "News", "post1.html")).Should().BeTrue();
    }

    // ------------------------------------------------------------------
    // Posts section: with pagination
    // ------------------------------------------------------------------

    [Fact]
    public void Generate_WithPagination_CreatesExpectedPageFiles()
    {
        WriteIndex();
        for (int i = 1; i <= 25; i++)
            WritePost("News", $"post{i}", $"# 2024-01-{i:D2} Post {i}\n\nBody {i}.");

        var result = Build(postsTitles: new List<string> { "News" }, postsPerPage: 10)
            .Generate("index.md");

        result.IsSuccess.Should().BeTrue();
        File.Exists(Path.Combine(_outputDir, "News.html")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDir, "News-2.html")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDir, "News-3.html")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDir, "News-4.html")).Should().BeFalse();
    }

    [Fact]
    public void Generate_WithPagination_Page1ContainsNewestPosts()
    {
        WriteIndex();
        for (int i = 1; i <= 25; i++)
            WritePost("News", $"post{i}", $"# 2024-01-{i:D2} Post {i}\n\nBody {i}.");

        Build(postsTitles: new List<string> { "News" }, postsPerPage: 10).Generate("index.md");

        var page1 = File.ReadAllText(Path.Combine(_outputDir, "News.html"));
        var page2 = File.ReadAllText(Path.Combine(_outputDir, "News-2.html"));
        var page3 = File.ReadAllText(Path.Combine(_outputDir, "News-3.html"));

        page1.Should().Contain("Post 25")
                     .And.Contain("Post 16")
                     .And.NotContain("Post 15");

        page2.Should().Contain("Post 15")
                     .And.Contain("Post 6");

        page3.Should().Contain("Post 5")
                     .And.Contain("Post 1")
                     .And.NotContain("Post 6");
    }

    [Fact]
    public void Generate_WithPagination_Page1LinksToPage2And3()
    {
        WriteIndex();
        for (int i = 1; i <= 25; i++)
            WritePost("News", $"post{i}", $"# 2024-01-{i:D2} Post {i}\n\nBody {i}.");

        Build(postsTitles: new List<string> { "News" }, postsPerPage: 10).Generate("index.md");

        var page1 = File.ReadAllText(Path.Combine(_outputDir, "News.html"));
        page1.Should().Contain("href=\"/News-2.html\"")
                     .And.Contain("href=\"/News-3.html\"")
                     .And.Contain("rel=\"next\" href=\"https://example.com/News-2.html\"");
    }

    [Fact]
    public void Generate_WithPagination_LastPageHasNoNextLink()
    {
        WriteIndex();
        for (int i = 1; i <= 25; i++)
            WritePost("News", $"post{i}", $"# 2024-01-{i:D2} Post {i}\n\nBody {i}.");

        Build(postsTitles: new List<string> { "News" }, postsPerPage: 10).Generate("index.md");

        var page3 = File.ReadAllText(Path.Combine(_outputDir, "News-3.html"));
        page3.Should().Contain("pagination-next disabled")
                     .And.NotContain("rel=\"next\"")
                     .And.Contain("rel=\"prev\" href=\"https://example.com/News-2.html\"");
    }

    [Fact]
    public void Generate_WithPagination_ExactlyOnePageNeeded_ProducesNoPaginationLinks()
    {
        WriteIndex();
        for (int i = 1; i <= 5; i++)
            WritePost("News", $"post{i}", $"# 2024-01-{i:D2} Post {i}\n\nBody {i}.");

        Build(postsTitles: new List<string> { "News" }, postsPerPage: 10).Generate("index.md");

        var page1 = File.ReadAllText(Path.Combine(_outputDir, "News.html"));
        page1.Should().NotContain("pagination-prev")
                     .And.NotContain("pagination-next");
    }

    // ------------------------------------------------------------------
    // Sorting
    // ------------------------------------------------------------------

    [Fact]
    public void Generate_PostsSortedNewestFirst()
    {
        WriteIndex();
        WritePost("News", "old", "# 2020-01-01 Old\n\nBody.");
        WritePost("News", "new", "# 2025-01-01 New\n\nBody.");
        WritePost("News", "middle", "# 2023-06-15 Middle\n\nBody.");

        Build(postsTitles: new List<string> { "News" }).Generate("index.md");

        var html = File.ReadAllText(Path.Combine(_outputDir, "News.html"));
        var newIdx = html.IndexOf("New</a>", StringComparison.Ordinal);
        var midIdx = html.IndexOf("Middle</a>", StringComparison.Ordinal);
        var oldIdx = html.IndexOf("Old</a>", StringComparison.Ordinal);

        newIdx.Should().BeLessThan(midIdx, "New should precede Middle");
        midIdx.Should().BeLessThan(oldIdx, "Middle should precede Old");
    }

    [Fact]
    public void Generate_UndatedPostsSortAfterDatedPosts()
    {
        WriteIndex();
        WritePost("News", "undated", "# Undated Post\n\nBody.");
        WritePost("News", "old", "# 2000-01-01 Old\n\nBody.");

        Build(postsTitles: new List<string> { "News" }).Generate("index.md");

        var html = File.ReadAllText(Path.Combine(_outputDir, "News.html"));
        var datedIdx = html.IndexOf("Old</a>", StringComparison.Ordinal);
        var undatedIdx = html.IndexOf("Undated Post</a>", StringComparison.Ordinal);

        datedIdx.Should().BeLessThan(undatedIdx, "dated posts should come before undated");
    }

    // ------------------------------------------------------------------
    // Navigation
    // ------------------------------------------------------------------

    [Fact]
    public void Generate_NavigationAlwaysLinksToPage1()
    {
        WriteIndex();
        for (int i = 1; i <= 25; i++)
            WritePost("News", $"post{i}", $"# 2024-01-{i:D2} Post {i}\n\nBody {i}.");

        Build(postsTitles: new List<string> { "News" }, postsPerPage: 10).Generate("index.md");

        var index = File.ReadAllText(Path.Combine(_outputDir, "index.html"));
        index.Should().Contain("href=\"/News.html\"")
                     .And.NotContain("href=\"/News-2.html\"")
                     .And.NotContain("href=\"/News-3.html\"");
    }

    [Fact]
    public void Generate_NavigationIncludesOtherPages()
    {
        WriteIndex();
        File.WriteAllText(Path.Combine(_inputDir, "About.md"), "# About\n\nAbout body.");

        Build(otherPages: new List<string> { "About.md" }).Generate("index.md");

        var index = File.ReadAllText(Path.Combine(_outputDir, "index.html"));
        index.Should().Contain("href=\"/About.html\"");
        File.Exists(Path.Combine(_outputDir, "About.html")).Should().BeTrue();
    }

    // ------------------------------------------------------------------
    // Multiple sections
    // ------------------------------------------------------------------

    [Fact]
    public void Generate_MultipleSections_EachPaginatesIndependently()
    {
        WriteIndex();
        for (int i = 1; i <= 15; i++)
            WritePost("News", $"n{i}", $"# 2024-01-{i:D2} News {i}\n\nBody.");
        for (int i = 1; i <= 25; i++)
            WritePost("Blog", $"b{i}", $"# 2024-02-{i:D2} Blog {i}\n\nBody.");

        Build(postsTitles: new List<string> { "News", "Blog" }, postsPerPage: 10)
            .Generate("index.md");

        File.Exists(Path.Combine(_outputDir, "News.html")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDir, "News-2.html")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDir, "News-3.html")).Should().BeFalse();

        File.Exists(Path.Combine(_outputDir, "Blog.html")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDir, "Blog-2.html")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDir, "Blog-3.html")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDir, "Blog-4.html")).Should().BeFalse();
    }

    // ------------------------------------------------------------------
    // RSS
    // ------------------------------------------------------------------

    [Fact]
    public void Generate_WithRssFeed_ProducesXmlWithAllPosts()
    {
        WriteIndex();
        for (int i = 1; i <= 15; i++)
            WritePost("News", $"post{i}", $"# 2024-01-{i:D2} Post {i}\n\nBody {i}.");

        Build(postsTitles: new List<string> { "News" }, postsPerPage: 10, rssFeed: true)
            .Generate("index.md");

        var xml = File.ReadAllText(Path.Combine(_outputDir, "News.xml"));

        xml.Should().StartWith("<?xml")
                   .And.Contain("<rss version=\"2.0\">")
                   .And.Contain("<title>Test Site - News</title>");

        CountOccurrences(xml, "<item>").Should().Be(15);
    }

    [Fact]
    public void Generate_WithoutRssFeed_ProducesNoXml()
    {
        WriteIndex();
        WritePost("News", "post1", "# 2024-01-01 Post 1\n\nBody.");

        Build(postsTitles: new List<string> { "News" }, rssFeed: false).Generate("index.md");

        File.Exists(Path.Combine(_outputDir, "News.xml")).Should().BeFalse();
    }

    // ------------------------------------------------------------------
    // Open Graph on generated pages
    // ------------------------------------------------------------------

    [Fact]
    public void Generate_PostPage_HasArticleTypeAndCleanTitle()
    {
        WriteIndex();
        WritePost("News", "post1", "# 2024-03-01 A Great Post\n\nBody here.");

        Build(postsTitles: new List<string> { "News" }).Generate("index.md");

        var html = File.ReadAllText(Path.Combine(_outputDir, "News", "post1.html"));

        html.Should().Contain("og:type\" content=\"article\"")
                    .And.Contain("og:title\" content=\"A Great Post\"")
                    .And.NotContain("2024-03-01");
    }

    [Fact]
    public void Generate_ListingPage_HasWebsiteType()
    {
        WriteIndex();
        WritePost("News", "post1", "# 2024-01-01 Post\n\nBody.");

        Build(postsTitles: new List<string> { "News" }).Generate("index.md");

        var html = File.ReadAllText(Path.Combine(_outputDir, "News.html"));
        html.Should().Contain("og:type\" content=\"website\"");
    }

    // ------------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------------

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0, idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) != -1)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }
    
    [Fact]
    public void Generate_CopiesNonMarkdownFoldersToOutput()
    {
        WriteIndex();
        Directory.CreateDirectory(Path.Combine(_inputDir, "fonts"));
        File.WriteAllText(Path.Combine(_inputDir, "fonts", "inter.woff2"), "fake-font");
        Directory.CreateDirectory(Path.Combine(_inputDir, "images"));
        File.WriteAllText(Path.Combine(_inputDir, "images", "logo.png"), "fake-png");

        Build().Generate("index.md");

        File.Exists(Path.Combine(_outputDir, "fonts", "inter.woff2")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDir, "images", "logo.png")).Should().BeTrue();
   }

   [Fact]
   public void Generate_DoesNotCopyMarkdownFoldersAsAssets()
   {
       WriteIndex();
       WritePost("News", "post1", "# 2024-01-01 Post\n\nBody.");

       Build(postsTitles: new List<string> { "News" }).Generate("index.md");

       // News/ should be rendered into output/News/*.html, not copied as a raw folder.
       Directory.Exists(Path.Combine(_outputDir, "News")).Should().BeTrue();
       File.Exists(Path.Combine(_outputDir, "News", "post1.md")).Should().BeFalse();
       File.Exists(Path.Combine(_outputDir, "News", "post1.html")).Should().BeTrue();
   }
   
   [Fact]
    public void Generate_EveryPageHasModeToggleMarkup()
    {
        WriteIndex();
        WritePost("News", "post1", "# 2024-01-01 Post\n\nBody.");
        File.WriteAllText(Path.Combine(_inputDir, "About.md"), "# About\n\nAbout.");

        Build(
            postsTitles: new List<string> { "News" },
            otherPages: new List<string> { "About.md" })
            .Generate("index.md");

        var pages = new[]
        {
            Path.Combine(_outputDir, "index.html"),
            Path.Combine(_outputDir, "News.html"),
            Path.Combine(_outputDir, "News", "post1.html"),
            Path.Combine(_outputDir, "About.html")
        };

        foreach (var page in pages)
        {
            var html = File.ReadAllText(page);
            html.Should().Contain("name=\"mode\"")
                        .And.Contain("id=\"mode_light\"")
                        .And.Contain("id=\"mode_dark\"")
                        .And.Contain("color-scheme-wrapper");
        }
    }
    
    [Fact]
    public void Generate_AboutPageWithTable_ProducesResponsiveTableMarkup()
    {
        WriteIndex();
        File.WriteAllText(Path.Combine(_inputDir, "About.md"),
            "# About\n\n| Name | Value |\n|------|-------|\n| Foo  | 42    |\n");

        Build(otherPages: new List<string> { "About.md" }).Generate("index.md");

        var html = File.ReadAllText(Path.Combine(_outputDir, "About.html"));
        html.Should().Contain("table-wrapper")
            .And.Contain("data-label=\"Name\"")
            .And.Contain("data-label=\"Value\"");
    }
}
