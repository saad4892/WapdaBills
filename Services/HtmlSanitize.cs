using Ganss.Xss;

namespace WapdaBill.Services;

public static class HtmlSanitize
{
    private static readonly HtmlSanitizer Sanitizer = Create();

    private static HtmlSanitizer Create()
    {
        var s = new HtmlSanitizer();
        s.AllowedTags.Clear();
        foreach (var tag in new[]
                 {
                     "p", "br", "strong", "b", "em", "i", "u", "ul", "ol", "li",
                     "h2", "h3", "h4", "a", "img", "table", "thead", "tbody", "tr", "th", "td",
                     "blockquote", "span", "div"
                 })
        {
            s.AllowedTags.Add(tag);
        }

        s.AllowedAttributes.Clear();
        foreach (var attr in new[] { "href", "src", "alt", "title", "width", "height", "class", "rel", "target" })
        {
            s.AllowedAttributes.Add(attr);
        }

        s.AllowedSchemes.Clear();
        s.AllowedSchemes.Add("http");
        s.AllowedSchemes.Add("https");
        s.AllowedSchemes.Add("mailto");
        return s;
    }

    public static string Clean(string? html) =>
        string.IsNullOrWhiteSpace(html) ? "" : Sanitizer.Sanitize(html);
}
