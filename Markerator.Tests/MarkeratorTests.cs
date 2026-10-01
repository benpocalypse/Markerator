using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace Markerator.Tests;

/// <summary>
/// Integration tests that exercise Program.Main end-to-end.
///
/// These tests change the process-wide current directory, so they must not run
/// in parallel with any other test that relies on the working directory.
/// The [Collection] attribute ensures xUnit serializes them.
/// </summary>
[Collection("Program working-directory tests")]
public class ProgramTests
{
    private readonly string _root;
    private readonly string _inputDir;
    private readonly string _outputDir;
    private readonly string _originalDir;

    public ProgramTests()
    {
        _originalDir = Directory.GetCurrentDirectory();
        _root = Path.Combine(
            Path.GetTempPath(),
            "markerator-prog-" + Guid.NewGuid().ToString("N"));
        _inputDir = Path.Combine(_root, "input");
        _outputDir = Path.Combine(_root, "output");
        Directory.CreateDirectory(_inputDir);
    }

    private void Cleanup()
    {
        try { Directory.SetCurrentDirectory(_originalDir); } catch { /* best effort */ }
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

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

    // ------------------------------------------------------------------
    // Happy paths
    // ------------------------------------------------------------------

    [Fact]
    public void Main_WithMinimalValidArgs_ReturnsZeroAndGeneratesIndex()
    {
        WriteIndex();
        try
        {
            Directory.SetCurrentDirectory(_root);

            var exit = Markerator.Main(new[]
            {
                "-t", "Test Site",
                "-u", "https://example.com",
                "-i", "index.md"
            });

            exit.Should().Be(0);
            File.Exists(Path.Combine(_outputDir, "index.html")).Should().BeTrue();
        }
        finally { Cleanup(); }
    }

    [Fact]
    public void Main_WithPosts_GeneratesSectionPages()
    {
        WriteIndex();
        WritePost("News", "post1", "# 2024-01-01 First\n\nBody one.");
        WritePost("News", "post2", "# 2024-01-02 Second\n\nBody two.");

        try
        {
            Directory.SetCurrentDirectory(_root);

            var exit = Markerator.Main(new[]
            {
                "-t", "Test Site",
                "-u", "https://example.com",
                "-i", "index.md",
                "-p", "true",
                "-pt", "News"
            });

            exit.Should().Be(0);
            File.Exists(Path.Combine(_outputDir, "News.html")).Should().BeTrue();
            File.Exists(Path.Combine(_outputDir, "News", "post1.html")).Should().BeTrue();
            File.Exists(Path.Combine(_outputDir, "News", "post2.html")).Should().BeTrue();
        }
        finally { Cleanup(); }
    }

    [Fact]
    public void Main_WithPagination_CreatesNumberedPages()
    {
        WriteIndex();
        for (int i = 1; i <= 25; i++)
            WritePost("News", $"post{i}", $"# 2024-01-{i:D2} Post {i}\n\nBody {i}.");

        try
        {
            Directory.SetCurrentDirectory(_root);

            var exit = Markerator.Main(new[]
            {
                "-t", "Test Site",
                "-u", "https://example.com",
                "-i", "index.md",
                "-p", "true",
                "-pt", "News",
                "-pp", "10"
            });

            exit.Should().Be(0);
            File.Exists(Path.Combine(_outputDir, "News.html")).Should().BeTrue();
            File.Exists(Path.Combine(_outputDir, "News-2.html")).Should().BeTrue();
            File.Exists(Path.Combine(_outputDir, "News-3.html")).Should().BeTrue();
            File.Exists(Path.Combine(_outputDir, "News-4.html")).Should().BeFalse();
        }
        finally { Cleanup(); }
    }

    [Fact]
    public void Main_WithRssFeed_ProducesXml()
    {
        WriteIndex();
        WritePost("News", "post1", "# 2024-01-01 Post\n\nBody.");

        try
        {
            Directory.SetCurrentDirectory(_root);

            var exit = Markerator.Main(new[]
            {
                "-t", "Test Site",
                "-u", "https://example.com",
                "-i", "index.md",
                "-p", "true",
                "-pt", "News",
                "-rss", "true"
            });

            exit.Should().Be(0);
            File.Exists(Path.Combine(_outputDir, "News.xml")).Should().BeTrue();
        }
        finally { Cleanup(); }
    }

    [Fact]
    public void Main_WithMultipleSections_GeneratesEachSection()
    {
        WriteIndex();
        WritePost("News", "n1", "# 2024-01-01 News One\n\nBody.");
        WritePost("Blog", "b1", "# 2024-02-01 Blog One\n\nBody.");

        try
        {
            Directory.SetCurrentDirectory(_root);

            var exit = Markerator.Main(new[]
            {
                "-t", "Test Site",
                "-u", "https://example.com",
                "-i", "index.md",
                "-p", "true",
                "-pt", "News,Blog"
            });

            exit.Should().Be(0);
            File.Exists(Path.Combine(_outputDir, "News.html")).Should().BeTrue();
            File.Exists(Path.Combine(_outputDir, "Blog.html")).Should().BeTrue();
        }
        finally { Cleanup(); }
    }

    [Fact]
    public void Main_WithOtherPages_GeneratesStandalonePage()
    {
        WriteIndex();
        File.WriteAllText(Path.Combine(_inputDir, "About.md"), "# About\n\nAbout body.");

        try
        {
            Directory.SetCurrentDirectory(_root);

            var exit = Markerator.Main(new[]
            {
                "-t", "Test Site",
                "-u", "https://example.com",
                "-i", "index.md",
                "-op", "About.md"
            });

            exit.Should().Be(0);
            File.Exists(Path.Combine(_outputDir, "About.html")).Should().BeTrue();
        }
        finally { Cleanup(); }
    }

    [Fact]
    public void Main_WithPostsEnabledButNoTitles_DefaultsToNewsSection()
    {
        WriteIndex();
        WritePost("News", "post1", "# 2024-01-01 Post\n\nBody.");

        try
        {
            Directory.SetCurrentDirectory(_root);

            var exit = Markerator.Main(new[]
            {
                "-t", "Test Site",
                "-u", "https://example.com",
                "-i", "index.md",
                "-p", "true"
            });

            exit.Should().Be(0);
            File.Exists(Path.Combine(_outputDir, "News.html")).Should().BeTrue();
        }
        finally { Cleanup(); }
    }

    [Fact]
    public void Main_NormalizesCommaSeparatedPostsTitles()
    {
        WriteIndex();
        WritePost("News", "n1", "# 2024-01-01 N1\n\nBody.");
        WritePost("Blog", "b1", "# 2024-02-01 B1\n\nBody.");
        WritePost("Updates", "u1", "# 2024-03-01 U1\n\nBody.");

        try
        {
            Directory.SetCurrentDirectory(_root);

            var exit = Markerator.Main(new[]
            {
                "-t", "Test Site",
                "-u", "https://example.com",
                "-i", "index.md",
                "-p", "true",
                "-pt", "News, Blog; Updates"
            });

            exit.Should().Be(0);
            File.Exists(Path.Combine(_outputDir, "News.html")).Should().BeTrue();
            File.Exists(Path.Combine(_outputDir, "Blog.html")).Should().BeTrue();
            File.Exists(Path.Combine(_outputDir, "Updates.html")).Should().BeTrue();
        }
        finally { Cleanup(); }
    }

    [Fact]
    public void Main_TrimsTrailingSlashFromBaseUrl()
    {
        WriteIndex();
        try
        {
            Directory.SetCurrentDirectory(_root);

            var exit = Markerator.Main(new[]
            {
                "-t", "Test Site",
                "-u", "https://example.com/",
                "-i", "index.md"
            });

            exit.Should().Be(0);

            var html = File.ReadAllText(Path.Combine(_outputDir, "index.html"));

            html.Should().NotContain("https://example.com//")
                        .And.Contain("og:url\" content=\"https://example.com/\"");
        }
        finally { Cleanup(); }
    }

    // ------------------------------------------------------------------
    // Error paths
    // ------------------------------------------------------------------

    [Fact]
    public void Main_WithMissingIndexFile_ReturnsNonZero()
    {
        try
        {
            Directory.SetCurrentDirectory(_root);

            var exit = Markerator.Main(new[]
            {
                "-t", "Test Site",
                "-u", "https://example.com",
                "-i", "index.md"
            });

            exit.Should().NotBe(0);
        }
        finally { Cleanup(); }
    }

    [Fact]
    public void Main_WithMissingInputDirectory_ReturnsNonZero()
    {
        Directory.Delete(_inputDir, recursive: true);

        try
        {
            Directory.SetCurrentDirectory(_root);

            var exit = Markerator.Main(new[]
            {
                "-t", "Test Site",
                "-u", "https://example.com",
                "-i", "index.md"
            });

            exit.Should().NotBe(0);
        }
        finally { Cleanup(); }
    }

    // ------------------------------------------------------------------
    // Defaults
    // ------------------------------------------------------------------

    [Fact]
    public void Main_WithNoPostsPerPage_DoesNotPaginate()
    {
        WriteIndex();
        for (int i = 1; i <= 25; i++)
            WritePost("News", $"post{i}", $"# 2024-01-{i:D2} Post {i}\n\nBody {i}.");

        try
        {
            Directory.SetCurrentDirectory(_root);

            var exit = Markerator.Main(new[]
            {
                "-t", "Test Site",
                "-u", "https://example.com",
                "-i", "index.md",
                "-p", "true",
                "-pt", "News"
            });

            exit.Should().Be(0);
            File.Exists(Path.Combine(_outputDir, "News.html")).Should().BeTrue();
            File.Exists(Path.Combine(_outputDir, "News-2.html")).Should().BeFalse();
        }
        finally { Cleanup(); }
    }

    [Fact]
    public void Main_WithExplicitZeroPostsPerPage_DoesNotPaginate()
    {
        WriteIndex();
        for (int i = 1; i <= 25; i++)
            WritePost("News", $"post{i}", $"# 2024-01-{i:D2} Post {i}\n\nBody {i}.");

        try
        {
            Directory.SetCurrentDirectory(_root);

            var exit = Markerator.Main(new[]
            {
                "-t", "Test Site",
                "-u", "https://example.com",
                "-i", "index.md",
                "-p", "true",
                "-pt", "News",
                "-pp", "0"
            });

            exit.Should().Be(0);
            File.Exists(Path.Combine(_outputDir, "News.html")).Should().BeTrue();
            File.Exists(Path.Combine(_outputDir, "News-2.html")).Should().BeFalse();
        }
        finally { Cleanup(); }
    }

    [Fact]
    public void Main_WithNoCss_StillGeneratesIndex()
    {
        WriteIndex();
        try
        {
            Directory.SetCurrentDirectory(_root);

            var exit = Markerator.Main(new[]
            {
                "-t", "Test Site",
                "-u", "https://example.com",
                "-i", "index.md"
            });

            exit.Should().Be(0);
            File.Exists(Path.Combine(_outputDir, "index.html")).Should().BeTrue();
        }
        finally { Cleanup(); }
    }
}

/// <summary>
/// Disables xUnit test-collection parallelism for the ProgramTests class.
/// </summary>
[CollectionDefinition("Program working-directory tests", DisableParallelization = true)]
public class ProgramWorkingDirectoryCollection
{
}
