using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WapdaBill.Data;
using WapdaBill.Models;

namespace WapdaBill.ViewComponents;

public class LatestContentViewComponent : ViewComponent
{
    private readonly AppDbContext _db;

    public LatestContentViewComponent(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var guides = await _db.BlogPosts
            .Where(post => post.IsPublished)
            .OrderByDescending(post => post.PublishedAt)
            .Take(6)
            .ToListAsync();
        return View(new LatestContentModel { Guides = guides });
    }
}

public sealed class LatestContentModel
{
    public required List<BlogPost> Guides { get; init; }
}
