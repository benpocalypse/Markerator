using FluentAssertions;
using Markerator.Helpers;
using Xunit;

namespace Markerator.Tests;

public class OpenGraphHelperTests
{
    [Fact]
    public void GenerateDescription_StripsMarkdownHeadings()
    {
        var md = "# My Heading\n\nThis is the body.";

        var desc = OpenGraphHelper.GenerateDescription(md, 200);

        desc.Should().NotContain("My Heading")
                   .And.Contain("This is the body.");
    }

    [Fact]
    public void GenerateDescription_RemovesImages()
    {
        var md = "![alt text](/img.png) Some real content follows.";

        var desc = OpenGraphHelper.GenerateDescription(md, 200);

        desc.Should().NotContain("alt text")
                   .And.NotContain("img.png")
                   .And.Contain("Some real content follows.");
    }

    [Fact]
    public void GenerateDescription_KeepsLinkTextButRemovesUrl()
    {
        var md = "See [the docs](https://example.com/docs) for details.";

        var desc = OpenGraphHelper.GenerateDescription(md, 200);

        desc.Should().Contain("the docs")
                   .And.NotContain("https://example.com");
    }

    [Fact]
    public void GenerateDescription_RemovesCodeBlocks()
    {
        var md = "Intro text.\n\n```csharp\nvar x = 1;\n```\n\nConclusion.";

        var desc = OpenGraphHelper.GenerateDescription(md, 200);

        desc.Should().NotContain("var x")
                   .And.Contain("Intro text.");
    }

    [Fact]
    public void GenerateDescription_RemovesInlineCode()
    {
        var md = "Use `dotnet run` to start.";

        var desc = OpenGraphHelper.GenerateDescription(md, 200);

        desc.Should().NotContain("`")
                   .And.Contain("Use")
                   .And.Contain("to start.");
    }

    [Fact]
    public void GenerateDescription_RemovesEmphasisMarkers()
    {
        var md = "This is **bold** and _italic_ and *also italic*.";

        var desc = OpenGraphHelper.GenerateDescription(md, 200);

        desc.Should().NotContain("**")
                   .And.NotContain("__")
                   .And.Contain("This is bold and italic and also italic.");
    }

    [Fact]
    public void GenerateDescription_TruncatesAtMaxLengthWithEllipsis()
    {
        var md = new string('a', 500);

        var desc = OpenGraphHelper.GenerateDescription(md, 50);

        desc.Length.Should().BeLessThanOrEqualTo(53);
        desc.Should().EndWith("...");
    }

    [Fact]
    public void GenerateDescription_ReturnsFirstSentenceWhenShorterThanMax()
    {
        var md = "First sentence. Second sentence. Third sentence.";

        var desc = OpenGraphHelper.GenerateDescription(md, 200);

        desc.Should().Be("First sentence.");
    }

    [Fact]
    public void GenerateDescription_EmptyInput_ReturnsEmpty()
    {
        OpenGraphHelper.GenerateDescription("", 200).Should().BeEmpty();
        OpenGraphHelper.GenerateDescription(null, 200).Should().BeEmpty();
    }

    [Fact]
    public void GenerateDescription_WhitespaceOnlyInput_ReturnsEmpty()
    {
        OpenGraphHelper.GenerateDescription("    \n\n   ", 200).Should().BeEmpty();
    }

    [Fact]
    public void GenerateForIndex_UsesBaseUrlWithoutTrailingSlash()
    {
        var og = OpenGraphHelper.GenerateForIndex(
            "Title", "https://example.com/", "Site", "Body content.");

        og.Url.Should().Be("https://example.com/");
        og.ImageUrl.Should().Be("https://example.com/images/og-image.png");
        og.Type.Should().Be("website");
    }

    [Fact]
    public void GenerateForPost_StripsDatePrefixFromTitle()
    {
        var og = OpenGraphHelper.GenerateForPost(
            "2024-03-01 My Big Post",
            "https://example.com",
            "My Site",
            "Body.",
            "News/post1.html");

        og.Title.Should().Be("My Big Post");
        og.Url.Should().Be("https://example.com/News/post1.html");
        og.Type.Should().Be("article");
    }

    [Fact]
    public void GenerateForPost_LeavesTitleAloneWhenNoDatePrefix()
    {
        var og = OpenGraphHelper.GenerateForPost(
            "Just a Title",
            "https://example.com",
            "My Site",
            "Body.",
            "News/post1.html");

        og.Title.Should().Be("Just a Title");
    }

    [Fact]
    public void GenerateForPage_SetsWebsiteTypeAndCorrectUrl()
    {
        var og = OpenGraphHelper.GenerateForPage(
            "About", "https://example.com", "My Site", "Body.", "About.html");

        og.Type.Should().Be("website");
        og.Url.Should().Be("https://example.com/About.html");
}
}
