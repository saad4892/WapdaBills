using System.Net;
using System.Text;

namespace WapdaBill.Services;

public static class PlainText
{
    public static string ToHtml(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var blocks = value.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var html = new StringBuilder();
        foreach (var block in blocks)
        {
            var lines = block.Split('\n').Select(line => line.TrimEnd()).Where(line => line.Length > 0).ToList();
            if (lines.Count == 0)
            {
                continue;
            }

            if (lines.All(line => line.StartsWith("- ", StringComparison.Ordinal)))
            {
                html.Append("<ul>");
                foreach (var line in lines)
                {
                    html.Append("<li>").Append(WebUtility.HtmlEncode(line[2..].Trim())).Append("</li>");
                }

                html.Append("</ul>");
                continue;
            }

            if (lines.Count == 1 && lines[0].StartsWith("## ", StringComparison.Ordinal))
            {
                html.Append("<h2>").Append(WebUtility.HtmlEncode(lines[0][3..].Trim())).Append("</h2>");
                continue;
            }

            html.Append("<p>");
            html.Append(string.Join("<br />", lines.Select(WebUtility.HtmlEncode)));
            html.Append("</p>");
        }

        return html.ToString();
    }
}
