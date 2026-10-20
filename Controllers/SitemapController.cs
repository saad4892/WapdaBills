using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WapdaBill.Data;
using WapdaBill.Services;

namespace WapdaBill.Controllers;

public class SitemapController : Controller
{
    private readonly AppDbContext _db;

    public SitemapController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Index()
    {
        var origin = $"{Request.Scheme}://{Request.Host}";
        var urls = SiteNav.AllPublicPaths()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => origin + path)
            .ToList();
        urls.AddRange(await _db.BlogPosts.Where(post => post.IsPublished).Select(post => origin + "/guides/" + post.Slug).ToListAsync());

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var document = new XDocument(
            new XElement(ns + "urlset",
                urls.Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(url => new XElement(ns + "url", new XElement(ns + "loc", url)))));
        return Content(document.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet("/robots.txt")]
    public IActionResult Robots()
    {
        var origin = $"{Request.Scheme}://{Request.Host}";
        var text = $"User-agent: *{Environment.NewLine}Allow: /{Environment.NewLine}Sitemap: {origin}/sitemap.xml{Environment.NewLine}";
        return Content(text, "text/plain", Encoding.UTF8);
    }
}
