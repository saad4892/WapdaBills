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
            .Take(3)
            .ToListAsync();
        var jobs = await _db.JobListings
            .Where(job => job.IsPublished)
            .OrderByDescending(job => job.PublishedAt)
            .Take(3)
            .ToListAsync();
        return View(new LatestContentModel { Guides = guides, Jobs = jobs });
    }
}

public sealed class LatestContentModel
{
    public required List<BlogPost> Guides { get; init; }

    public required List<JobListing> Jobs { get; init; }
}
