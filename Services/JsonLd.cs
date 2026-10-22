using System.Text.Json;
using WapdaBill.Models;

namespace WapdaBill.Services;

public static class JsonLd
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string Serialize(object value)
    {
        var json = JsonSerializer.Serialize(value, Options);
        return json
            .Replace("\"context\"", "\"@context\"", StringComparison.Ordinal)
            .Replace("\"type\"", "\"@type\"", StringComparison.Ordinal)
            .Replace("\"id\"", "\"@id\"", StringComparison.Ordinal);
    }

    public static object Organization(string origin) => new
    {
        context = "https://schema.org",
        type = "Organization",
        name = "WAPDA Bills",
        url = origin + "/",
        logo = origin + "/favicon.svg",
        description = "Free Pakistan electricity bill checker for WAPDA DISCOs."
    };

    public static object WebSite(string origin) => new
    {
        context = "https://schema.org",
        type = "WebSite",
        name = "WAPDA Bills",
        url = origin + "/",
        potentialAction = new
        {
            type = "SearchAction",
            target = origin + "/?company={company}&number={number}",
            query_input = "required name=number"
        }
    };

    public static object WebApplication(string origin) => new
    {
        context = "https://schema.org",
        type = "WebApplication",
        name = "Pakistan Electricity Bill Checker",
        url = origin + "/",
        applicationCategory = "UtilitiesApplication",
        operatingSystem = "Any",
        offers = new { type = "Offer", price = "0", priceCurrency = "PKR" }
    };

    public static object FaqPage(IEnumerable<FaqItem> faqs) => new
    {
        context = "https://schema.org",
        type = "FAQPage",
        mainEntity = faqs.Select(item => new
        {
            type = "Question",
            name = item.Question,
            acceptedAnswer = new { type = "Answer", text = item.Answer }
        }).ToArray()
    };

    public static object BreadcrumbList(string origin, IReadOnlyList<(string Name, string Path)> crumbs) => new
    {
        context = "https://schema.org",
        type = "BreadcrumbList",
        itemListElement = crumbs.Select((c, i) => new
        {
            type = "ListItem",
            position = i + 1,
            name = c.Name,
            item = origin + (c.Path.StartsWith('/') ? c.Path : "/" + c.Path)
        }).ToArray()
    };

    public static object BlogPosting(string origin, BlogPost post)
    {
        var url = origin + "/guides/" + post.Slug;
        return new
        {
            context = "https://schema.org",
            type = "BlogPosting",
            headline = post.Title,
            description = string.IsNullOrWhiteSpace(post.MetaDescription) ? post.Excerpt : post.MetaDescription,
            datePublished = post.PublishedAt.ToString("yyyy-MM-dd"),
            dateModified = post.UpdatedAt.ToString("yyyy-MM-dd"),
            author = new { type = "Organization", name = "WAPDA Bills" },
            publisher = new
            {
                type = "Organization",
                name = "WAPDA Bills",
                logo = new { type = "ImageObject", url = origin + "/favicon.svg" }
            },
            image = string.IsNullOrWhiteSpace(post.ImagePath) ? origin + "/img/og-default.svg" : (post.ImagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? post.ImagePath : origin + post.ImagePath),
            mainEntityOfPage = url,
            url
        };
    }
}
