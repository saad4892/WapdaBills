using System.Diagnostics;
using System.Security.Claims;
using System.Text;
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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        AppDbContext db,
        IConfiguration config,
        IWebHostEnvironment env,
        IHttpClientFactory httpClientFactory,
        ILogger<AdminController> logger)
    {
        _db = db;
        _config = config;
        _env = env;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("/admin")]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Admin";
        ViewBag.Guides = await _db.BlogPosts.CountAsync();
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

    /// <summary>
    /// Admin-only outbound connectivity probe for MonsterASP / PITC diagnosis.
    /// Does not accept or echo consumer bill numbers.
    /// </summary>
    [HttpGet("/admin/pitc-check")]
    public async Task<IActionResult> PitcCheck(CancellationToken cancellationToken)
    {
        var portal = await ProbeAsync(
            "portal_get",
            PitcOutbound.PortalClientName,
            $"{PitcOutbound.PortalBaseUrl}/mepcobill",
            HttpMethod.Get,
            content: null,
            cancellationToken);

        var snaps = await ProbeAsync(
            "snaps_post",
            PitcOutbound.SnapsClientName,
            PitcOutbound.SnapsUrl,
            HttpMethod.Post,
            new StringContent(
                """{"REF_NO":"00000000000000","BILL_MONTH":"01-2020"}""",
                Encoding.UTF8,
                "application/json"),
            cancellationToken);

        _logger.LogInformation(
            "Admin PITC check. PortalOk={PortalOk} PortalCategory={PortalCategory} PortalMs={PortalMs} " +
            "SnapsOk={SnapsOk} SnapsCategory={SnapsCategory} SnapsMs={SnapsMs}",
            portal.ReachedHost,
            portal.Category,
            portal.ElapsedMs,
            snaps.ReachedHost,
            snaps.Category,
            snaps.ElapsedMs);

        return Json(new
        {
            checkedAtUtc = DateTime.UtcNow,
            environment = _env.EnvironmentName,
            portal,
            snaps,
            hostingHint = portal.ReachedHost && snaps.ReachedHost
                ? "Outbound HTTPS to PITC works from this host."
                : "Outbound HTTPS to PITC failed from this host. Ask MonsterASP to allow outbound HTTPS to bill.pitc.com.pk and usersnap.pitc.com.pk (ports 443), and check DNS/TLS from the app pool."
        });
    }

    private sealed record PitcProbeResult(
        string Operation,
        string Host,
        string Path,
        bool ReachedHost,
        int? StatusCode,
        long ElapsedMs,
        string Category,
        string? BodyPreview = null,
        string? ExceptionType = null,
        string? Message = null,
        string? Inner = null);

    private async Task<PitcProbeResult> ProbeAsync(
        string operation,
        string clientName,
        string url,
        HttpMethod method,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        var uri = new Uri(url);
        var client = _httpClientFactory.CreateClient(clientName);
        using var request = new HttpRequestMessage(method, uri) { Content = content };
        var sw = Stopwatch.StartNew();
        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var preview = body.Length > 180 || body.Contains('<', StringComparison.Ordinal)
                ? $"(omitted len={body.Length})"
                : body;

            return new PitcProbeResult(
                operation,
                uri.Host,
                uri.AbsolutePath,
                ReachedHost: true,
                (int)response.StatusCode,
                sw.ElapsedMilliseconds,
                response.IsSuccessStatusCode ? "ok" : ((int)response.StatusCode >= 500 ? "http_5xx" : "http_4xx"),
                BodyPreview: preview);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            var category = PitcHttpLogging.Classify(ex, statusCode: null, requestAborted: cancellationToken.IsCancellationRequested);
            PitcHttpLogging.LogOutboundFailure(
                _logger, ex, operation, uri.Host, uri.AbsolutePath, null,
                sw.ElapsedMilliseconds, null, null, cancellationToken.IsCancellationRequested);

            var inner = ex.InnerException is null
                ? null
                : $"{ex.InnerException.GetType().Name}: {(ex.InnerException.Message.Length > 200 ? ex.InnerException.Message[..200] + "…" : ex.InnerException.Message)}";

            return new PitcProbeResult(
                operation,
                uri.Host,
                uri.AbsolutePath,
                ReachedHost: false,
                StatusCode: null,
                sw.ElapsedMilliseconds,
                category,
                ExceptionType: ex.GetType().FullName,
                Message: ex.Message.Length > 240 ? ex.Message[..240] + "…" : ex.Message,
                Inner: inner);
        }
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
