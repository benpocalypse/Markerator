using FluentAssertions;
using Markerator.Abstractions;
using Xunit;

namespace Markerator.Tests;

public class OpenGraphDataTests
{
    [Fact]
    public void GenerateMetaTags_EmitsStandardOgProperties()
    {
        var og = new OpenGraphData
        {
            Title = "My Page",
            Description = "A description.",
            ImageUrl = "https://example.com/img.jpg",
            Url = "https://example.com/page.html",
            Type = "article",
            SiteName = "My Site"
        };

        var html = og.GenerateMetaTags();

        html.Should().Contain("<meta property=\"og:title\" content=\"My Page\" />")
                    .And.Contain("<meta property=\"og:description\" content=\"A description.\" />")
                    .And.Contain("<meta property=\"og:image\" content=\"https://example.com/img.jpg\" />")
                    .And.Contain("<meta property=\"og:url\" content=\"https://example.com/page.html\" />")
                    .And.Contain("<meta property=\"og:type\" content=\"article\" />")
                    .And.Contain("<meta property=\"og:site_name\" content=\"My Site\" />");
    }

    [Fact]
    public void GenerateMetaTags_EmitsTwitterCardTags()
    {
        var og = new OpenGraphData
        {
            Title = "My Page",
            Description = "A description.",
            ImageUrl = "https://example.com/img.jpg"
        };

        var html = og.GenerateMetaTags();

        html.Should().Contain("<meta name=\"twitter:card\" content=\"summary_large_image\" />")
                    .And.Contain("<meta name=\"twitter:title\" content=\"My Page\" />")
                    .And.Contain("<meta name=\"twitter:description\" content=\"A description.\" />")
                    .And.Contain("<meta name=\"twitter:image\" content=\"https://example.com/img.jpg\" />");
    }

    [Fact]
    public void GenerateMetaTags_OmitsTagsWithNullValues()
    {
        var og = new OpenGraphData { Title = "Only Title" };

        var html = og.GenerateMetaTags();

        html.Should().Contain("og:title")
                    .And.NotContain("og:description")
                    .And.NotContain("og:image")
                    .And.NotContain("og:url")
                    .And.NotContain("og:site_name");
    }

    [Fact]
    public void GenerateMetaTags_OmitsTagsWithEmptyStrings()
    {
        var og = new OpenGraphData
        {
            Title = "Only Title",
            Description = string.Empty,
            ImageUrl = string.Empty
        };

        var html = og.GenerateMetaTags();

        html.Should().Contain("og:title")
                    .And.NotContain("og:description")
                    .And.NotContain("og:image");
    }

    [Fact]
    public void GenerateMetaTags_HtmlEncodesSpecialCharacters()
    {
        var og = new OpenGraphData
        {
            Title = "Tom & Jerry <3 \"quotes\"",
            Description = "5 < 10 && 10 > 5"
        };

        var html = og.GenerateMetaTags();

        html.Should().Contain("Tom &amp; Jerry &lt;3 &quot;quotes&quot;")
                    .And.Contain("5 &lt; 10 &amp;&amp; 10 &gt; 5")
                    .And.NotContain("Tom & Jerry")
                    .And.NotContain("5 < 10");
    }

    [Fact]
    public void GenerateMetaTags_DefaultTypeIsWebsite()
    {
        var og = new OpenGraphData { Title = "x" };

        og.Type.Should().Be("website");
        og.GenerateMetaTags().Should().Contain("og:type\" content=\"website\"");
    }
}
