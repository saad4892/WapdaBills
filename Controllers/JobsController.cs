using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WapdaBill.Data;

namespace WapdaBill.Controllers;

public class JobsController : Controller
{
    private readonly AppDbContext _db;

    public JobsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("/careers")]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Electricity careers in Pakistan";
        ViewData["Description"] = "Where LESCO, MEPCO, PESCO, HAZECO and PITC publish their own recruitment notices. These pages do not take applications or fees.";
        ViewData["Keywords"] = "LESCO jobs, MEPCO jobs, PESCO jobs, HAZECO jobs, PITC careers, WAPDA recruitment";
        var jobs = await _db.JobListings
            .Where(job => job.IsPublished)
            .OrderByDescending(job => job.PublishedAt)
            .ToListAsync();
        return View(jobs);
    }

    [HttpGet("/careers/{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var job = await _db.JobListings.FirstOrDefaultAsync(item => item.Id == id && item.IsPublished);
        if (job is null)
        {
            return NotFound();
        }

        ViewData["Title"] = job.Title;
        ViewData["Description"] = string.IsNullOrWhiteSpace(job.MetaDescription) ? job.Summary : job.MetaDescription;
        ViewData["Keywords"] = job.MetaKeywords;
        return View(job);
    }
}
