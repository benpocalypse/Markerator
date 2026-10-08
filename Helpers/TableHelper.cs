using HtmlAgilityPack;

namespace Markerator.Helpers;

/// <summary>
/// Post-processes rendered HTML to make markdown-generated tables
/// mobile-friendly. Each &lt;table&gt; is wrapped in a scrollable
/// container, and every &lt;td&gt; receives a <c>data-label</c>
/// attribute containing its column header text. On narrow viewports,
/// CSS can then stack the table cells as cards, with each cell
/// showing its column header inline.
/// </summary>
public static class TableHelper
{
    /// <summary>
    /// Finds every &lt;table&gt; in the HTML and prepares it for
    /// responsive display. Returns the modified HTML.
    /// </summary>
    /// <param name="html">The rendered HTML fragment.</param>
    /// <returns>The HTML with tables prepared for responsive display.</returns>
    public static string MakeTablesResponsive(string html)
    {
        if (string.IsNullOrEmpty(html) || !html.Contains("<table"))
        {
            return html;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var tables = doc.DocumentNode.SelectNodes("//table");
        if (tables == null)
        {
            return html;
        }

        foreach (var table in tables)
        {
            // Gather the header labels from the first <thead> row.
            var headerLabels = new List<string>();
            var headerCells = table.SelectNodes(".//thead/tr/th");
            if (headerCells != null)
            {
                foreach (var th in headerCells)
                {
                    headerLabels.Add(th.InnerText.Trim());
                }
            }

            // If there are no <thead> labels, fall back to the first row's
            // <th> cells, if any.
            if (headerLabels.Count == 0)
            {
                var firstRowHeaders = table.SelectNodes(".//tr[1]/th");
                if (firstRowHeaders != null)
                {
                    foreach (var th in firstRowHeaders)
                    {
                        headerLabels.Add(th.InnerText.Trim());
                    }
                }
            }

            // Apply data-label to every <td>. If a row has fewer cells
            // than the header, only the cells present are labeled.
            var bodyRows = table.SelectNodes(".//tbody/tr") ?? table.SelectNodes(".//tr");
            if (bodyRows != null)
            {
                foreach (var row in bodyRows)
                {
                    var cells = row.SelectNodes("td");
                    if (cells == null) continue;

                    for (int i = 0; i < cells.Count; i++)
                    {
                        if (i < headerLabels.Count && !string.IsNullOrEmpty(headerLabels[i]))
                        {
                            cells[i].SetAttributeValue("data-label", headerLabels[i]);
                        }
                    }
                }
            }

            // Wrap the <table> in a <div class="table-wrapper"> so the
            // table can scroll horizontally on narrow screens when the
            // stacked-card layout isn't triggered (e.g. very wide tables).
            var wrapper = doc.CreateElement("div");
            wrapper.AddClass("table-wrapper");

            var parent = table.ParentNode;
            parent.ReplaceChild(wrapper, table);
            wrapper.AppendChild(table);
        }

        return doc.DocumentNode.OuterHtml;
    }
}
