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
    private readonly IConfiguration _config;

    public SitemapController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Index()
    {
        var origin = SiteUrls.PublicOrigin(_config, Request);
        var staticLastMod = DateTime.UtcNow.Date;
        var urls = new List<(string Loc, DateTime LastMod)>();

        foreach (var path in SiteNav.AllPublicPaths().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            urls.Add((origin + path, staticLastMod));
        }

        var pages = await _db.ContentPages
            .Where(p => p.IsPublished && p.Robots.Contains("index"))
            .Select(p => new { p.Slug, p.UpdatedAt })
            .ToListAsync();
        urls.AddRange(pages.Select(p => (origin + "/" + p.Slug, p.UpdatedAt.Date)));

        var discos = await _db.Discos
            .Where(d => d.IsPublished)
            .Select(d => new { d.PublicSlug, d.UpdatedAt })
            .ToListAsync();
        urls.AddRange(discos.Select(d => (origin + "/" + d.PublicSlug, d.UpdatedAt.Date)));

        var hubs = await _db.RegionHubs
            .Where(r => r.IsPublished)
            .Select(r => new { r.Path, r.UpdatedAt })
            .ToListAsync();
        urls.AddRange(hubs.Select(r => (origin + "/" + r.Path.TrimStart('/'), r.UpdatedAt.Date)));

        var posts = await _db.BlogPosts
            .Where(post => post.IsPublished)
            .Select(post => new { post.Slug, post.UpdatedAt, post.PublishedAt })
            .ToListAsync();
        urls.AddRange(posts.Select(post => (
            origin + "/guides/" + post.Slug,
            (post.UpdatedAt == default ? post.PublishedAt : post.UpdatedAt).Date
        )));

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var document = new XDocument(
            new XElement(ns + "urlset",
                urls
                    .GroupBy(u => u.Loc, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.OrderByDescending(x => x.LastMod).First())
                    .OrderBy(u => u.Loc, StringComparer.OrdinalIgnoreCase)
                    .Select(url => new XElement(ns + "url",
                        new XElement(ns + "loc", url.Loc),
                        new XElement(ns + "lastmod", url.LastMod.ToString("yyyy-MM-dd"))))));

        return Content(document.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet("/robots.txt")]
    [ResponseCache(Duration = 3600)]
    public IActionResult Robots()
    {
        var origin = SiteUrls.PublicOrigin(_config, Request);
        var text =
            $"User-agent: *{Environment.NewLine}" +
            $"Allow: /{Environment.NewLine}" +
            $"Disallow: /admin{Environment.NewLine}" +
            $"Disallow: /admin/{Environment.NewLine}" +
            $"Disallow: /Bill/Result{Environment.NewLine}" +
            $"Disallow: /Bill/Document{Environment.NewLine}" +
            $"Disallow: /Bill/Missing{Environment.NewLine}" +
            $"Disallow: /Home/Error{Environment.NewLine}" +
            $"Disallow: /Home/StatusCodePage{Environment.NewLine}" +
            $"{Environment.NewLine}" +
            $"Sitemap: {origin}/sitemap.xml{Environment.NewLine}";
        return Content(text, "text/plain", Encoding.UTF8);
    }
}
