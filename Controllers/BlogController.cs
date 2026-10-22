using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WapdaBill.Data;

namespace WapdaBill.Controllers;

public class BlogController : Controller
{
    private readonly AppDbContext _db;

    public BlogController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("/guides")]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Electricity Bill Guides Pakistan | WAPDA";
        ViewData["Description"] = "Guides for reference numbers, U/R codes, DISCO coverage, and how to check a Pakistan electricity bill online.";
        ViewData["Keywords"] = "electricity bill guide, reference number, DISCO coverage, WAPDA jobs scam";
        var posts = await _db.BlogPosts
            .Where(post => post.IsPublished)
            .OrderByDescending(post => post.PublishedAt)
            .ToListAsync();
        return View(posts);
    }

    [HttpGet("/guides/{slug}")]
    public async Task<IActionResult> Detail(string slug)
    {
        var post = await _db.BlogPosts.FirstOrDefaultAsync(item => item.Slug == slug && item.IsPublished);
        if (post is null)
        {
            return NotFound();
        }

        ViewData["Title"] = post.Title;
        ViewData["Description"] = string.IsNullOrWhiteSpace(post.MetaDescription) ? post.Excerpt : post.MetaDescription;
        ViewData["Keywords"] = post.MetaKeywords;
        ViewBag.GuideFaqs = await _db.GuideFaqs
            .AsNoTracking()
            .Where(f => f.BlogPostId == post.Id)
            .OrderBy(f => f.SortOrder)
            .ToListAsync();
        return View(post);
    }
}
