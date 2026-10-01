using System.Collections.Generic;
using System.Linq;
using Markerator;
using FluentAssertions;
using Markerator.Helpers;
using Xunit;

namespace Markerator.Tests;

public class PaginationHelperTests
{
    [Fact]
    public void Paginate_WithEmptyList_ReturnsSingleEmptyPage()
    {
        var pages = PaginationHelper.Paginate(new List<int>(), 10);

        pages.Should().ContainSingle();
        pages[0].Should().BeEmpty();
    }

    [Fact]
    public void Paginate_WithZeroPageSize_ReturnsSinglePageWithAllItems()
    {
        var items = Enumerable.Range(1, 25).ToList();

        var pages = PaginationHelper.Paginate(items, 0);

        pages.Should().ContainSingle();
        pages[0].Should().HaveCount(25);
    }

    [Fact]
    public void Paginate_WithNegativePageSize_ReturnsSinglePageWithAllItems()
    {
        var items = Enumerable.Range(1, 25).ToList();

        var pages = PaginationHelper.Paginate(items, -5);

        pages.Should().ContainSingle();
        pages[0].Should().HaveCount(25);
    }

    [Theory]
    [InlineData(25, 10, 3)]
    [InlineData(20, 10, 2)]
    [InlineData(21, 10, 3)]
    [InlineData(5, 10, 1)]
    [InlineData(1, 10, 1)]
    [InlineData(30, 5, 6)]
    public void Paginate_WithPageSize_SplitsIntoExpectedNumberOfPages(
        int itemCount, int pageSize, int expectedPages)
    {
        var items = Enumerable.Range(1, itemCount).ToList();

        var pages = PaginationHelper.Paginate(items, pageSize);

        pages.Should().HaveCount(expectedPages);
    }

    [Fact]
    public void Paginate_PreservesItemOrderAcrossPages()
    {
        var items = Enumerable.Range(1, 25).ToList();

        var pages = PaginationHelper.Paginate(items, 10);

        pages[0].Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        pages[1].Should().Equal(11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
        pages[2].Should().Equal(21, 22, 23, 24, 25);
    }

    [Theory]
    [InlineData("News", 1, "News.html")]
    [InlineData("News", 2, "News-2.html")]
    [InlineData("News", 3, "News-3.html")]
    [InlineData("Blog", 10, "Blog-10.html")]
    [InlineData("News", 0, "News.html")]
    public void GetPageFileName_ProducesExpectedName(string section, int page, string expected)
    {
        PaginationHelper.GetPageFileName(section, page).Should().Be(expected);
    }

    [Fact]
    public void GeneratePaginationLinks_WithSinglePage_ReturnsEmpty()
    {
        var html = PaginationHelper.GeneratePaginationLinks("News", 1, 1, "https://example.com");

        html.Should().BeEmpty();
    }

    [Fact]
    public void GeneratePaginationLinks_OnFirstPage_HasNoPrevAndHasNext()
    {
        var html = PaginationHelper.GeneratePaginationLinks("News", 1, 3, "https://example.com");

        html.Should().Contain("pagination-prev disabled")
                    .And.Contain("href=\"/News-2.html\"")
                    .And.Contain("aria-current=\"page\">1<")
                    .And.NotContain("href=\"/News-0.html\"");
    }

    [Fact]
    public void GeneratePaginationLinks_OnLastPage_HasPrevAndNoNext()
    {
        var html = PaginationHelper.GeneratePaginationLinks("News", 3, 3, "https://example.com");

        html.Should().Contain("href=\"/News-2.html\"")
                    .And.Contain("pagination-next disabled");
    }

    [Fact]
    public void GeneratePaginationLinks_OnMiddlePage_HasBothLinks()
    {
        var html = PaginationHelper.GeneratePaginationLinks("News", 2, 3, "https://example.com");

        html.Should().Contain("href=\"/News.html\"")
                    .And.Contain("href=\"/News-3.html\"")
                    .And.Contain("aria-current=\"page\">2<");
    }

    [Fact]
    public void GeneratePaginationLinks_NumbersEveryPage()
    {
        var html = PaginationHelper.GeneratePaginationLinks("News", 2, 4, "https://example.com");

        html.Should().Contain("href=\"/News.html\"")
                    .And.Contain("aria-current=\"page\">2<")
                    .And.Contain("href=\"/News-3.html\"")
                    .And.Contain("href=\"/News-4.html\"");
    }

    [Fact]
    public void GenerateRelLinks_OnFirstPage_OnlyNextPresent()
    {
        var html = PaginationHelper.GenerateRelLinks("News", 1, 3, "https://example.com");

        html.Should().NotContain("rel=\"prev\"")
                    .And.Contain("rel=\"next\" href=\"https://example.com/News-2.html\"");
    }

    [Fact]
    public void GenerateRelLinks_OnLastPage_OnlyPrevPresent()
    {
        var html = PaginationHelper.GenerateRelLinks("News", 3, 3, "https://example.com");

        html.Should().Contain("rel=\"prev\" href=\"https://example.com/News-2.html\"")
                    .And.NotContain("rel=\"next\"");
    }

    [Fact]
    public void GenerateRelLinks_OnMiddlePage_BothPresent()
    {
        var html = PaginationHelper.GenerateRelLinks("News", 2, 3, "https://example.com");

        html.Should().Contain("rel=\"prev\" href=\"https://example.com/News.html\"")
                    .And.Contain("rel=\"next\" href=\"https://example.com/News-3.html\"");
    }

    [Fact]
    public void GenerateRelLinks_StripsTrailingSlashFromBaseUrl()
    {
        var html = PaginationHelper.GenerateRelLinks("News", 1, 2, "https://example.com/");

        html.Should().Contain("https://example.com/News-2.html")
                    .And.NotContain("https://example.com//News-2.html");
    }
}
