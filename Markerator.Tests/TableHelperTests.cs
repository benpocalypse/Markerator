using FluentAssertions;
using Xunit;
using Markerator.Helpers;

namespace Markerator.Tests;

public class TableHelperTests
{
    [Fact]
    public void MakeTablesResponsive_WrapsTableAndAddsDataLabels()
    {
        var html = "<table><thead><tr><th>Name</th><th>Value</th></tr></thead>" +
                   "<tbody><tr><td>Foo</td><td>42</td></tr></tbody></table>";

        var result = TableHelper.MakeTablesResponsive(html);

        result.Should().Contain("table-wrapper")
            .And.Contain("data-label=\"Name\"")
            .And.Contain("data-label=\"Value\"");
    }

    [Fact]
    public void MakeTablesResponsive_LeavesNonTableHtmlUnchanged()
    {
        var html = "<p>No tables here.</p>";

        var result = TableHelper.MakeTablesResponsive(html);

        result.Should().Be(html);
    }
}