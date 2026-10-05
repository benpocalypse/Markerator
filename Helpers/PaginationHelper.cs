using System.Text;

namespace Markerator.Helpers
{
    public static class PaginationHelper
    {
        public static List<List<T>> Paginate<T>(IList<T> items, int pageSize)
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

        public static string GetPageFileName(string sectionName, int pageNumber)
        {
            return pageNumber <= 1
                ? $"{sectionName}.html"
                : $"{sectionName}-{pageNumber}.html";
        }

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
}
