using System.Text;

namespace Markerator.Helpers;

/// <summary>
/// Utility methods for splitting a list of items into pages and rendering
/// the navigation markup that links those pages together.
/// </summary>
public static class PaginationHelper
{
    /// <summary>
    /// Splits a list into pages of at most <paramref name="pageSize"/> items.
    /// If <paramref name="pageSize"/> is 0 or negative, or if the list is empty,
    /// a single page is returned containing all items (or none).
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The items to paginate.</param>
    /// <param name="pageSize">The maximum number of items per page.</param>
    /// <returns>A list of pages, each containing a subset of the source items.</returns>
    public static List<List<T>> Paginate<T>(IList<T>? items, int pageSize)
    {
        var result = new List<List<T>>();

        if (items == null || items.Count == 0)
        {
            result.Add(new List<T>());
            return result;
        }

        if (pageSize <= 0)
        {
            result.Add(items.ToList());
            return result;
        }

        for (int i = 0; i < items.Count; i += pageSize)
        {
            result.Add(items.Skip(i).Take(pageSize).ToList());
        }

        return result;
    }

    /// <summary>
    /// Returns the output file name for a given page of a section. Page 1 is
    /// the canonical section page (e.g. <c>News.html</c>); subsequent pages
    /// are suffixed with the page number (e.g. <c>News-2.html</c>).
    /// </summary>
    /// <param name="sectionName">The section name (e.g. News, Blog).</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <returns>The output file name for the requested page.</returns>
    public static string GetPageFileName(string sectionName, int pageNumber)
    {
        return pageNumber <= 1
            ? $"{sectionName}.html"
            : $"{sectionName}-{pageNumber}.html";
    }

    /// <summary>
    /// Renders the pagination navigation markup shown at the bottom of a
    /// section page. Includes previous/next links, numbered page links, and
    /// a "current page" indicator. Returns an empty string if there is only
    /// one page.
    /// </summary>
    /// <param name="sectionName">The section name (e.g. News).</param>
    /// <param name="currentPage">The 1-based index of the page being rendered.</param>
    /// <param name="totalPages">The total number of pages in the section.</param>
    /// <param name="baseUrl">The site base URL, used for building absolute URLs.</param>
    /// <returns>The rendered pagination markup, or an empty string if not needed.</returns>
    public static string GeneratePaginationLinks(
        string sectionName, int currentPage, int totalPages, string baseUrl)
    {
        if (totalPages <= 1) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("<nav class=\"pagination\" aria-label=\"Pagination\">");

        if (currentPage > 1)
        {
            var prevFile = GetPageFileName(sectionName, currentPage - 1);
            sb.AppendLine($"  <a class=\"pagination-prev\" href=\"/{prevFile}\">&laquo; Previous</a>");
        }
        else
        {
            sb.AppendLine("  <span class=\"pagination-prev disabled\">&laquo; Previous</span>");
        }

        sb.AppendLine("  <span class=\"pagination-pages\">");
        for (int i = 1; i <= totalPages; i++)
        {
            var file = GetPageFileName(sectionName, i);
            if (i == currentPage)
            {
                sb.AppendLine($"    <span class=\"pagination-current\" aria-current=\"page\">{i}</span>");
            }
            else
            {
                sb.AppendLine($"    <a href=\"/{file}\">{i}</a>");
            }
        }
        sb.AppendLine("  </span>");

        if (currentPage < totalPages)
        {
            var nextFile = GetPageFileName(sectionName, currentPage + 1);
            sb.AppendLine($"  <a class=\"pagination-next\" href=\"/{nextFile}\">Next &raquo;</a>");
        }
        else
        {
            sb.AppendLine("  <span class=\"pagination-next disabled\">Next &raquo;</span>");
        }

        sb.AppendLine("</nav>");
        return sb.ToString();
    }

    /// <summary>
    /// Renders <c>rel="prev"</c> and <c>rel="next"</c> link elements for a
    /// paginated series, suitable for inclusion in the page <c>&lt;head&gt;</c>.
    /// These help search engines understand the sequence of paginated pages.
    /// </summary>
    /// <param name="sectionName">The section name (e.g. News).</param>
    /// <param name="currentPage">The 1-based index of the page being rendered.</param>
    /// <param name="totalPages">The total number of pages in the section.</param>
    /// <param name="baseUrl">The site base URL, used for building absolute URLs.</param>
    /// <returns>The rendered <c>&lt;link&gt;</c> elements.</returns>
    public static string GenerateRelLinks(
        string sectionName, int currentPage, int totalPages, string baseUrl)
    {
        var sb = new StringBuilder();
        var cleanBase = baseUrl.TrimEnd('/');

        if (currentPage > 1)
        {
            var prevFile = GetPageFileName(sectionName, currentPage - 1);
            sb.AppendLine($"<link rel=\"prev\" href=\"{cleanBase}/{prevFile}\" />");
        }

        if (currentPage < totalPages)
        {
            var nextFile = GetPageFileName(sectionName, currentPage + 1);
            sb.AppendLine($"<link rel=\"next\" href=\"{cleanBase}/{nextFile}\" />");
        }

        return sb.ToString();
    }
}
