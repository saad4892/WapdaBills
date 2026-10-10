using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WapdaBill.Data;
using WapdaBill.Models;
using WapdaBill.Services;

namespace WapdaBill.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public AdminController(AppDbContext db, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    [HttpGet("/admin")]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Admin";
        ViewBag.Guides = await _db.BlogPosts.CountAsync();
        ViewBag.Jobs = await _db.JobListings.CountAsync();
        return View();
    }

    [HttpGet("/admin/login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = "Admin login";
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost("/admin/login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl)
    {
        var expectedUser = _config["Admin:Username"] ?? "";
        var expectedPassword = _config["Admin:Password"] ?? "";
        var userOk = string.Equals(username?.Trim(), expectedUser, StringComparison.Ordinal);
        var passwordOk = string.Equals(password, expectedPassword, StringComparison.Ordinal);
        if (!userOk || !passwordOk || expectedPassword.Length == 0)
        {
            ViewData["Title"] = "Admin login";
            ViewData["Error"] = "Those details do not match.";
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, expectedUser)],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(new ClaimsPrincipal(identity));
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("/admin/guides")]
    public async Task<IActionResult> Guides()
    {
        ViewData["Title"] = "Manage guides";
        var posts = await _db.BlogPosts.OrderByDescending(post => post.UpdatedAt).ToListAsync();
        return View(posts);
    }

    [HttpGet("/admin/guides/edit/{id:int?}")]
    public async Task<IActionResult> EditGuide(int? id)
    {
        ViewData["Title"] = id is null ? "New guide" : "Edit guide";
        if (id is null)
        {
            return View(new BlogPost { IsPublished = true });
        }

        var post = await _db.BlogPosts.FindAsync(id);
        return post is null ? NotFound() : View(post);
    }

    [HttpPost("/admin/guides/edit/{id:int?}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditGuide(int? id, BlogPost input, IFormFile? cover, bool removeImage)
    {
        input.Title = input.Title.Trim();
        input.Excerpt = input.Excerpt.Trim();
        input.Body = input.Body.Trim();
        input.MetaDescription = input.MetaDescription.Trim();
        input.MetaKeywords = input.MetaKeywords.Trim();
        input.Slug = Slugger.Make(string.IsNullOrWhiteSpace(input.Slug) ? input.Title : input.Slug);
        if (input.Title.Length == 0 || input.Body.Length == 0 || input.Slug.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Title and body are required.");
        }

        var slugTaken = await _db.BlogPosts.AnyAsync(post => post.Slug == input.Slug && post.Id != input.Id);
        if (slugTaken)
        {
            ModelState.AddModelError(nameof(input.Slug), "That address is already used.");
        }

        var imageError = await SaveImageAsync(cover);
        if (imageError.Error is not null)
        {
            ModelState.AddModelError(string.Empty, imageError.Error);
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = input.Id == 0 ? "New guide" : "Edit guide";
            return View(input);
        }

        BlogPost post;
        if (input.Id == 0)
        {
            post = input;
            post.PublishedAt = DateTime.UtcNow;
            _db.BlogPosts.Add(post);
        }
        else
        {
            post = await _db.BlogPosts.FindAsync(input.Id) ?? throw new InvalidOperationException();
            post.Title = input.Title;
            post.Slug = input.Slug;
            post.Excerpt = input.Excerpt;
            post.Body = input.Body;
            post.MetaDescription = input.MetaDescription;
            post.MetaKeywords = input.MetaKeywords;
            post.IsPublished = input.IsPublished;
        }

        if (removeImage)
        {
            post.ImagePath = null;
        }

        if (imageError.Path is not null)
        {
            post.ImagePath = imageError.Path;
        }

        post.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Guides));
    }

    [HttpPost("/admin/guides/delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGuide(int id)
    {
        var post = await _db.BlogPosts.FindAsync(id);
        if (post is not null)
        {
            _db.BlogPosts.Remove(post);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Guides));
    }

    [HttpGet("/admin/careers")]
    public async Task<IActionResult> Careers()
    {
        ViewData["Title"] = "Manage careers";
        var jobs = await _db.JobListings.OrderByDescending(job => job.UpdatedAt).ToListAsync();
        return View(jobs);
    }

    [HttpGet("/admin/careers/edit/{id:int?}")]
    public async Task<IActionResult> EditCareer(int? id)
    {
        ViewData["Title"] = id is null ? "New career notice" : "Edit career notice";
        if (id is null)
        {
            return View(new JobListing { IsPublished = true, EmploymentType = "Career notice" });
        }

        var job = await _db.JobListings.FindAsync(id);
        return job is null ? NotFound() : View(job);
    }

    [HttpPost("/admin/careers/edit/{id:int?}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCareer(int? id, JobListing input, IFormFile? cover, bool removeImage)
    {
        input.Title = input.Title.Trim();
        input.Organization = input.Organization.Trim();
        input.Location = input.Location.Trim();
        input.EmploymentType = input.EmploymentType.Trim();
        input.Summary = input.Summary.Trim();
        input.Body = input.Body.Trim();
        input.ApplyUrl = string.IsNullOrWhiteSpace(input.ApplyUrl) ? null : input.ApplyUrl.Trim();
        input.MetaDescription = input.MetaDescription.Trim();
        input.MetaKeywords = input.MetaKeywords.Trim();
        if (input.Title.Length == 0 || input.Body.Length == 0 || input.Organization.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Title, organization, and body are required.");
        }

        if (!string.IsNullOrWhiteSpace(input.ApplyUrl) &&
            (!Uri.TryCreate(input.ApplyUrl, UriKind.Absolute, out var applyUri) ||
             applyUri.Scheme is not "http" and not "https"))
        {
            ModelState.AddModelError(nameof(input.ApplyUrl), "Use a full http or https link.");
        }

        var imageError = await SaveImageAsync(cover);
        if (imageError.Error is not null)
        {
            ModelState.AddModelError(string.Empty, imageError.Error);
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = input.Id == 0 ? "New career notice" : "Edit career notice";
            return View(input);
        }

        JobListing job;
        if (input.Id == 0)
        {
            job = input;
            job.PublishedAt = DateTime.UtcNow;
            _db.JobListings.Add(job);
        }
        else
        {
            job = await _db.JobListings.FindAsync(input.Id) ?? throw new InvalidOperationException();
            job.Title = input.Title;
            job.Organization = input.Organization;
            job.Location = input.Location;
            job.EmploymentType = string.IsNullOrWhiteSpace(input.EmploymentType) ? "Career notice" : input.EmploymentType;
            job.Summary = input.Summary;
            job.Body = input.Body;
            job.ApplyUrl = input.ApplyUrl;
            job.Deadline = input.Deadline;
            job.MetaDescription = input.MetaDescription;
            job.MetaKeywords = input.MetaKeywords;
            job.IsPublished = input.IsPublished;
        }

        if (removeImage)
        {
            job.ImagePath = null;
        }

        if (imageError.Path is not null)
        {
            job.ImagePath = imageError.Path;
        }

        job.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Careers));
    }

    [HttpPost("/admin/careers/delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCareer(int id)
    {
        var job = await _db.JobListings.FindAsync(id);
        if (job is not null)
        {
            _db.JobListings.Remove(job);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Careers));
    }

    [HttpGet("/admin/seo")]
    public async Task<IActionResult> Seo()
    {
        ViewData["Title"] = "Search listing";
        var settings = await _db.SiteSettings.FindAsync(1) ?? new SiteSetting { Id = 1 };
        return View(settings);
    }

    [HttpPost("/admin/seo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Seo(SiteSetting input)
    {
        input.SiteTitle = input.SiteTitle.Trim();
        input.MetaDescription = input.MetaDescription.Trim();
        input.MetaKeywords = input.MetaKeywords.Trim();
        if (input.SiteTitle.Length == 0 || input.MetaDescription.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Title and description are required.");
            ViewData["Title"] = "Search listing";
            return View(input);
        }

        var settings = await _db.SiteSettings.FindAsync(1);
        if (settings is null)
        {
            input.Id = 1;
            _db.SiteSettings.Add(input);
        }
        else
        {
            settings.SiteTitle = input.SiteTitle;
            settings.MetaDescription = input.MetaDescription;
            settings.MetaKeywords = input.MetaKeywords;
        }

        await _db.SaveChangesAsync();
        TempData["Saved"] = "Search listing saved.";
        return RedirectToAction(nameof(Seo));
    }

    private async Task<(string? Path, string? Error)> SaveImageAsync(IFormFile? cover)
    {
        if (cover is null || cover.Length == 0)
        {
            return (null, null);
        }

        var extension = System.IO.Path.GetExtension(cover.FileName).ToLowerInvariant();
        if (extension is not ".jpg" and not ".jpeg" and not ".png" and not ".webp")
        {
            return (null, "Use a JPG, PNG, or WebP image.");
        }

        if (cover.Length > 2_000_000)
        {
            return (null, "Images must be under 2 MB.");
        }

        var folder = System.IO.Path.Combine(_env.WebRootPath, "media");
        Directory.CreateDirectory(folder);
        var name = $"{Guid.NewGuid():N}{extension}";
        await using var stream = System.IO.File.Create(System.IO.Path.Combine(folder, name));
        await cover.CopyToAsync(stream);
        return ("/media/" + name, null);
    }
}
