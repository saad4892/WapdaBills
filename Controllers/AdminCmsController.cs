using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WapdaBill.Data;
using WapdaBill.Models;
using WapdaBill.Models.Cms;
using WapdaBill.Services;

namespace WapdaBill.Controllers;

[Authorize(Policy = "EditorOrAdmin")]
public class AdminCmsController : Controller
{
    private readonly AppDbContext _db;
    private readonly CmsQuery _cms;
    private readonly MediaService _media;

    public AdminCmsController(AppDbContext db, CmsQuery cms, MediaService media)
    {
        _db = db;
        _cms = cms;
        _media = media;
    }

    private async Task AuditAsync(string action, string entity, string? id, string? detail = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Username = User.Identity?.Name ?? "unknown",
            Action = action,
            EntityType = entity,
            EntityId = id,
            Detail = detail,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    // ---- Settings ----
    [HttpGet("/admin/settings")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Settings()
    {
        ViewData["Title"] = "Site settings";
        var settings = await _db.SiteSettings.FirstOrDefaultAsync(s => s.Id == 1)
                       ?? new SiteSetting { Id = 1 };
        return View("~/Views/Admin/Settings.cshtml", settings);
    }

    [HttpPost("/admin/settings")]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(SiteSetting input)
    {
        ViewData["Title"] = "Site settings";
        var settings = await _db.SiteSettings.FirstOrDefaultAsync(s => s.Id == 1);
        if (settings is null)
        {
            settings = new SiteSetting { Id = 1 };
            _db.SiteSettings.Add(settings);
        }

        settings.SiteName = (input.SiteName ?? "").Trim();
        settings.SiteTitle = (input.SiteTitle ?? "").Trim();
        settings.MetaDescription = (input.MetaDescription ?? "").Trim();
        settings.MetaKeywords = (input.MetaKeywords ?? "").Trim();
        settings.GoogleSearchConsoleVerification = SanitizeToken(input.GoogleSearchConsoleVerification);
        settings.Ga4MeasurementId = SanitizeGa4(input.Ga4MeasurementId);
        settings.GtmContainerId = SanitizeGtm(input.GtmContainerId);
        settings.LogoMediaId = input.LogoMediaId;
        settings.FaviconMediaId = input.FaviconMediaId;
        settings.DefaultOgImageMediaId = input.DefaultOgImageMediaId;
        await _db.SaveChangesAsync();
        _cms.Invalidate();
        await AuditAsync("update", "SiteSettings", "1");
        TempData["Ok"] = "Settings saved.";
        return RedirectToAction(nameof(Settings));
    }

    // ---- Media ----
    [HttpGet("/admin/media")]
    public async Task<IActionResult> Media()
    {
        ViewData["Title"] = "Media library";
        var items = await _db.MediaAssets.OrderByDescending(m => m.CreatedAt).Take(200).ToListAsync();
        return View("~/Views/Admin/Media.cshtml", items);
    }

    [HttpPost("/admin/media/upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MediaUpload(IFormFile? file, string? altText)
    {
        if (file is null)
        {
            TempData["Error"] = "Choose an image file.";
            return RedirectToAction(nameof(Media));
        }

        var asset = await _media.SaveUploadAsync(file, altText);
        if (asset is null)
        {
            TempData["Error"] = "Upload rejected. Use JPG/PNG/WEBP/GIF/SVG under 5 MB.";
            return RedirectToAction(nameof(Media));
        }

        await AuditAsync("create", "MediaAsset", asset.Id.ToString());
        TempData["Ok"] = "Uploaded " + asset.StoragePath;
        return RedirectToAction(nameof(Media));
    }

    [HttpPost("/admin/media/delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MediaDelete(int id)
    {
        if (!await _media.TryDeleteAsync(id))
        {
            TempData["Error"] = "Could not delete (missing or still referenced).";
        }
        else
        {
            await AuditAsync("delete", "MediaAsset", id.ToString());
            TempData["Ok"] = "Media deleted.";
        }

        return RedirectToAction(nameof(Media));
    }

    // ---- Nav ----
    [HttpGet("/admin/nav")]
    public async Task<IActionResult> Nav()
    {
        ViewData["Title"] = "Navigation";
        var menus = await _db.NavMenus.Include(m => m.Items).OrderBy(m => m.SortOrder).ToListAsync();
        return View("~/Views/Admin/Nav.cshtml", menus);
    }

    [HttpPost("/admin/nav/item")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NavSaveItem(int id, int navMenuId, string title, string href, string? description, int sortOrder, bool isVisible)
    {
        NavItem item;
        if (id > 0)
        {
            item = await _db.NavItems.FindAsync(id) ?? new NavItem();
            if (item.Id == 0)
            {
                return NotFound();
            }
        }
        else
        {
            item = new NavItem { NavMenuId = navMenuId };
            _db.NavItems.Add(item);
        }

        item.Title = (title ?? "").Trim();
        item.Href = (href ?? "").Trim();
        item.Description = description?.Trim();
        item.SortOrder = sortOrder;
        item.IsVisible = isVisible;
        await _db.SaveChangesAsync();
        _cms.Invalidate();
        await AuditAsync("save", "NavItem", item.Id.ToString());
        return RedirectToAction(nameof(Nav));
    }

    // ---- Footer / social ----
    [HttpGet("/admin/footer")]
    public async Task<IActionResult> Footer()
    {
        ViewData["Title"] = "Footer";
        ViewBag.Copy = await _db.FooterCopies.FirstOrDefaultAsync(c => c.Id == 1) ?? new FooterCopy { Id = 1 };
        ViewBag.Columns = await _db.FooterColumns.Include(c => c.Links).OrderBy(c => c.SortOrder).ToListAsync();
        // Columns typed as FooterColumnEntity
        ViewBag.Social = await _db.SocialLinks.OrderBy(s => s.SortOrder).ToListAsync();
        return View("~/Views/Admin/Footer.cshtml");
    }

    [HttpPost("/admin/footer/copy")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FooterCopySave(FooterCopy input)
    {
        var copy = await _db.FooterCopies.FirstOrDefaultAsync(c => c.Id == 1);
        if (copy is null)
        {
            copy = new FooterCopy { Id = 1 };
            _db.FooterCopies.Add(copy);
        }

        copy.BrandName = input.BrandName?.Trim() ?? "";
        copy.BrandTag = input.BrandTag?.Trim() ?? "";
        copy.Mission = input.Mission?.Trim() ?? "";
        copy.Disclaimer = input.Disclaimer?.Trim() ?? "";
        copy.CopyrightTemplate = string.IsNullOrWhiteSpace(input.CopyrightTemplate)
            ? "© {year} WAPDA Bills. All rights reserved."
            : input.CopyrightTemplate.Trim();
        await _db.SaveChangesAsync();
        _cms.Invalidate();
        TempData["Ok"] = "Footer copy saved.";
        return RedirectToAction(nameof(Footer));
    }

    [HttpPost("/admin/footer/social")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SocialSave(int id, string platform, string label, string href, int sortOrder, bool isVisible)
    {
        SocialLinkEntity row;
        if (id > 0)
        {
            row = await _db.SocialLinks.FindAsync(id) ?? new SocialLinkEntity();
            if (row.Id == 0) return NotFound();
        }
        else
        {
            row = new SocialLinkEntity();
            _db.SocialLinks.Add(row);
        }

        row.Platform = platform?.Trim() ?? "";
        row.Label = label?.Trim() ?? "";
        row.Href = href?.Trim() ?? "";
        row.SortOrder = sortOrder;
        row.IsVisible = isVisible;
        await _db.SaveChangesAsync();
        _cms.Invalidate();
        return RedirectToAction(nameof(Footer));
    }

    // ---- Pages ----
    [HttpGet("/admin/pages")]
    public async Task<IActionResult> Pages()
    {
        ViewData["Title"] = "Pages";
        return View("~/Views/Admin/Pages.cshtml", await _db.ContentPages.OrderBy(p => p.Slug).ToListAsync());
    }

    [HttpGet("/admin/pages/edit/{id:int?}")]
    public async Task<IActionResult> PageEdit(int? id)
    {
        ViewData["Title"] = id is null ? "New page" : "Edit page";
        var page = id is null
            ? new ContentPage { IsPublished = true, Template = "Generic", Language = "en", Robots = "index, follow" }
            : await _db.ContentPages.FindAsync(id);
        if (page is null) return NotFound();
        return View("~/Views/Admin/PageEdit.cshtml", page);
    }

    [HttpPost("/admin/pages/edit/{id:int?}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PageEdit(int? id, ContentPage input)
    {
        ContentPage page;
        string? oldSlug = null;
        if (id is > 0)
        {
            page = await _db.ContentPages.FindAsync(id) ?? new ContentPage();
            if (page.Id == 0) return NotFound();
            oldSlug = page.Slug;
        }
        else
        {
            page = new ContentPage { CreatedAt = DateTime.UtcNow };
            _db.ContentPages.Add(page);
        }

        var newSlug = Slugger.Make(string.IsNullOrWhiteSpace(input.Slug) ? input.Title : input.Slug);
        if (!string.IsNullOrWhiteSpace(oldSlug) && !string.Equals(oldSlug, newSlug, StringComparison.OrdinalIgnoreCase)
            && page.IsPublished)
        {
            await EnsureRedirectAsync("/" + oldSlug, "/" + newSlug);
        }

        page.Slug = newSlug;
        page.Template = string.IsNullOrWhiteSpace(input.Template) ? "Generic" : input.Template.Trim();
        page.Title = input.Title?.Trim() ?? "";
        page.H1 = string.IsNullOrWhiteSpace(input.H1) ? page.Title : input.H1.Trim();
        page.BodyHtml = HtmlSanitize.Clean(input.BodyHtml);
        page.IntroHtml = HtmlSanitize.Clean(input.IntroHtml);
        page.MetaTitle = input.MetaTitle?.Trim() ?? page.Title;
        page.MetaDescription = input.MetaDescription?.Trim() ?? "";
        page.MetaKeywords = input.MetaKeywords?.Trim() ?? "";
        page.CanonicalPath = string.IsNullOrWhiteSpace(input.CanonicalPath) ? "/" + page.Slug : input.CanonicalPath.Trim();
        page.Robots = string.IsNullOrWhiteSpace(input.Robots) ? "index, follow" : input.Robots.Trim();
        page.Language = "en";
        page.IsPublished = input.IsPublished;
        page.UpdatedAt = DateTime.UtcNow;
        page.OgImageMediaId = input.OgImageMediaId;
        await _db.SaveChangesAsync();
        _cms.Invalidate();
        await AuditAsync("save", "ContentPage", page.Id.ToString(), page.Slug);
        TempData["Ok"] = "Page saved.";
        return RedirectToAction(nameof(Pages));
    }

    // ---- Discos ----
    [HttpGet("/admin/discos")]
    public async Task<IActionResult> Discos()
    {
        ViewData["Title"] = "DISCOs";
        return View("~/Views/Admin/Discos.cshtml", await _db.Discos.OrderBy(d => d.SortOrder).ToListAsync());
    }

    [HttpGet("/admin/discos/edit/{id:int}")]
    public async Task<IActionResult> DiscoEdit(int id)
    {
        var disco = await _db.Discos.FindAsync(id);
        if (disco is null) return NotFound();
        ViewData["Title"] = "Edit " + disco.ShortName;
        return View("~/Views/Admin/DiscoEdit.cshtml", disco);
    }

    [HttpPost("/admin/discos/edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DiscoEdit(int id, DiscoEntity input)
    {
        var disco = await _db.Discos.FindAsync(id);
        if (disco is null) return NotFound();

        var oldSlug = disco.PublicSlug;
        disco.ShortName = input.ShortName?.Trim() ?? disco.ShortName;
        disco.FullName = input.FullName?.Trim() ?? disco.FullName;
        disco.Coverage = input.Coverage?.Trim() ?? "";
        disco.PublicSlug = (input.PublicSlug ?? disco.PublicSlug).Trim().Trim('/');
        disco.IntroHtml = HtmlSanitize.Clean(input.IntroHtml);
        disco.BodyHtml = HtmlSanitize.Clean(input.BodyHtml);
        disco.MetaTitle = input.MetaTitle?.Trim() ?? "";
        disco.MetaDescription = input.MetaDescription?.Trim() ?? "";
        disco.MetaKeywords = input.MetaKeywords?.Trim() ?? "";
        disco.Color = string.IsNullOrWhiteSpace(input.Color) ? disco.Color : input.Color.Trim();
        disco.Note = input.Note?.Trim();
        disco.ExternalUrl = input.ExternalUrl?.Trim();
        disco.SortOrder = input.SortOrder;
        disco.IsPublished = input.IsPublished;
        disco.LogoMediaId = input.LogoMediaId;
        // Operational fields — Admin only
        if (User.IsInRole("Admin"))
        {
            disco.PitcSlug = input.PitcSlug?.Trim();
            disco.UsesPitc = input.UsesPitc;
            disco.RegionsJson = string.IsNullOrWhiteSpace(input.RegionsJson) ? disco.RegionsJson : input.RegionsJson;
        }

        disco.UpdatedAt = DateTime.UtcNow;
        if (!string.Equals(oldSlug, disco.PublicSlug, StringComparison.OrdinalIgnoreCase) && disco.IsPublished)
        {
            await EnsureRedirectAsync("/" + oldSlug, "/" + disco.PublicSlug);
        }

        await _db.SaveChangesAsync();
        _cms.Invalidate();
        await AuditAsync("save", "Disco", disco.Id.ToString(), disco.Code);
        TempData["Ok"] = "DISCO saved.";
        return RedirectToAction(nameof(Discos));
    }

    // ---- Homepage ----
    [HttpGet("/admin/homepage")]
    public async Task<IActionResult> Homepage()
    {
        ViewData["Title"] = "Homepage";
        var sections = await _db.HomepageSections.Include(s => s.Items).OrderBy(s => s.SortOrder).ToListAsync();
        return View("~/Views/Admin/Homepage.cshtml", sections);
    }

    [HttpPost("/admin/homepage/item")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HomepageItemSave(int id, int homepageSectionId, string title, string body, string? href, int sortOrder, bool isVisible)
    {
        HomepageItem item;
        if (id > 0)
        {
            item = await _db.HomepageItems.FindAsync(id) ?? new HomepageItem();
            if (item.Id == 0) return NotFound();
        }
        else
        {
            item = new HomepageItem { HomepageSectionId = homepageSectionId };
            _db.HomepageItems.Add(item);
        }

        item.Title = title?.Trim() ?? "";
        item.Body = body?.Trim() ?? "";
        item.Href = href?.Trim();
        item.SortOrder = sortOrder;
        item.IsVisible = isVisible;
        await _db.SaveChangesAsync();
        _cms.Invalidate();
        return RedirectToAction(nameof(Homepage));
    }

    // ---- Redirects / 404 ----
    [HttpGet("/admin/redirects")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Redirects()
    {
        ViewData["Title"] = "Redirects";
        ViewBag.Rules = await _db.RedirectRules.OrderByDescending(r => r.Id).Take(200).ToListAsync();
        ViewBag.NotFound = await _db.NotFoundLogs.OrderByDescending(n => n.OccurredAt).Take(100).ToListAsync();
        return View("~/Views/Admin/Redirects.cshtml");
    }

    [HttpPost("/admin/redirects")]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RedirectCreate(string fromPath, string toUrl, int statusCode = 301, bool isActive = true)
    {
        fromPath = NormalizePath(fromPath);
        toUrl = (toUrl ?? "").Trim();
        if (string.IsNullOrWhiteSpace(fromPath) || string.IsNullOrWhiteSpace(toUrl)
            || string.Equals(fromPath, toUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Invalid redirect (check paths / loop).";
            return RedirectToAction(nameof(Redirects));
        }

        if (await _db.RedirectRules.AnyAsync(r => r.FromPath == fromPath))
        {
            TempData["Error"] = "A redirect from that path already exists.";
            return RedirectToAction(nameof(Redirects));
        }

        _db.RedirectRules.Add(new RedirectRule
        {
            FromPath = fromPath,
            ToUrl = toUrl,
            StatusCode = statusCode is 301 or 302 ? statusCode : 301,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        await AuditAsync("create", "RedirectRule", fromPath);
        TempData["Ok"] = "Redirect saved.";
        return RedirectToAction(nameof(Redirects));
    }

    // ---- Export / Import ----
    [HttpGet("/admin/export")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Export()
    {
        // Flat scalar queries only — assemble nested JSON in memory so EF never returns entity graphs.
        var menus = await _db.NavMenus.AsNoTracking()
            .OrderBy(m => m.SortOrder)
            .Select(m => new { m.Id, m.Key, m.Label, m.SortOrder, m.IsVisible })
            .ToListAsync();
        var navItems = await _db.NavItems.AsNoTracking()
            .OrderBy(i => i.SortOrder)
            .Select(i => new { i.Id, i.NavMenuId, i.ParentId, i.Title, i.Href, i.Description, i.SortOrder, i.IsVisible, i.IsMega })
            .ToListAsync();
        var footerCols = await _db.FooterColumns.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => new { c.Id, c.Title, c.SortOrder, c.IsVisible })
            .ToListAsync();
        var footerLinks = await _db.FooterLinks.AsNoTracking()
            .OrderBy(l => l.SortOrder)
            .Select(l => new { l.Id, l.FooterColumnId, l.Title, l.Href, l.SortOrder, l.IsVisible })
            .ToListAsync();
        var homeSections = await _db.HomepageSections.AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .Select(s => new { s.Id, s.Key, s.Title, s.Lead, s.SortOrder, s.IsVisible })
            .ToListAsync();
        var homeItems = await _db.HomepageItems.AsNoTracking()
            .OrderBy(i => i.SortOrder)
            .Select(i => new { i.Id, i.HomepageSectionId, i.Title, i.Body, i.Href, i.ExtraJson, i.SortOrder, i.IsVisible })
            .ToListAsync();
        var siteSettings = await _db.SiteSettings.AsNoTracking()
            .Select(s => new
            {
                s.Id, s.SiteName, s.SiteTitle, s.MetaDescription, s.MetaKeywords,
                s.LogoMediaId, s.FaviconMediaId, s.DefaultOgImageMediaId,
                s.GoogleSearchConsoleVerification, s.Ga4MeasurementId, s.GtmContainerId
            }).ToListAsync();
        var socialLinks = await _db.SocialLinks.AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .Select(s => new { s.Id, s.Platform, s.Label, s.Href, s.SortOrder, s.IsVisible })
            .ToListAsync();
        var footerCopy = await _db.FooterCopies.AsNoTracking()
            .Select(c => new { c.Id, c.BrandName, c.BrandTag, c.Mission, c.Disclaimer, c.CopyrightTemplate })
            .ToListAsync();
        var contentPages = await _db.ContentPages.AsNoTracking()
            .Select(p => new
            {
                p.Id, p.Slug, p.Template, p.Title, p.H1, p.BodyHtml, p.IntroHtml,
                p.MetaTitle, p.MetaDescription, p.MetaKeywords, p.CanonicalPath,
                p.OgImageMediaId, p.Robots, p.Language, p.IsPublished, p.CreatedAt, p.UpdatedAt
            }).ToListAsync();
        var discos = await _db.Discos.AsNoTracking()
            .Select(d => new
            {
                d.Id, d.Code, d.ShortName, d.FullName, d.PublicSlug, d.Coverage, d.RegionsJson,
                d.PitcSlug, d.UsesPitc, d.Color, d.ExternalUrl, d.Note, d.IntroHtml, d.BodyHtml,
                d.LogoMediaId, d.MetaTitle, d.MetaDescription, d.MetaKeywords, d.SortOrder,
                d.IsPublished, d.UpdatedAt
            }).ToListAsync();
        var regionHubs = await _db.RegionHubs.AsNoTracking()
            .Select(r => new
            {
                r.Id, r.Path, r.RegionKey, r.Label, r.H1, r.IntroHtml, r.BodyHtml,
                r.MetaTitle, r.MetaDescription, r.MetaKeywords, r.IsPublished, r.UpdatedAt
            }).ToListAsync();
        var faqs = await _db.Faqs.AsNoTracking()
            .Select(f => new { f.Id, f.Scope, f.Question, f.AnswerHtml, f.SortOrder, f.IsPublished })
            .ToListAsync();
        var guides = await _db.BlogPosts.AsNoTracking()
            .Select(g => new
            {
                g.Id, g.Title, g.Slug, g.Excerpt, g.Body, g.MetaDescription, g.MetaKeywords,
                g.ImagePath, g.IsPublished, g.PublishedAt, g.UpdatedAt
            }).ToListAsync();
        var media = await _db.MediaAssets.AsNoTracking()
            .Select(m => new
            {
                m.Id, m.FileName, m.StoragePath, m.ContentType, m.ByteSize,
                m.Width, m.Height, m.AltText, m.CreatedAt
            }).ToListAsync();
        var redirects = await _db.RedirectRules.AsNoTracking()
            .Select(r => new { r.Id, r.FromPath, r.ToUrl, r.StatusCode, r.IsActive, r.CreatedAt })
            .ToListAsync();

        var payload = new
        {
            exportedAt = DateTime.UtcNow,
            siteSettings,
            navMenus = menus.Select(m => new
            {
                m.Id,
                m.Key,
                m.Label,
                m.SortOrder,
                m.IsVisible,
                Items = navItems.Where(i => i.NavMenuId == m.Id).Select(i => new
                {
                    i.Id, i.ParentId, i.Title, i.Href, i.Description, i.SortOrder, i.IsVisible, i.IsMega
                }).ToList()
            }).ToList(),
            footerColumns = footerCols.Select(c => new
            {
                c.Id,
                c.Title,
                c.SortOrder,
                c.IsVisible,
                Links = footerLinks.Where(l => l.FooterColumnId == c.Id).Select(l => new
                {
                    l.Id, l.Title, l.Href, l.SortOrder, l.IsVisible
                }).ToList()
            }).ToList(),
            socialLinks,
            footerCopy,
            contentPages,
            discos,
            regionHubs,
            homepageSections = homeSections.Select(s => new
            {
                s.Id,
                s.Key,
                s.Title,
                s.Lead,
                s.SortOrder,
                s.IsVisible,
                Items = homeItems.Where(i => i.HomepageSectionId == s.Id).Select(i => new
                {
                    i.Id, i.Title, i.Body, i.Href, i.ExtraJson, i.SortOrder, i.IsVisible
                }).ToList()
            }).ToList(),
            faqs,
            guides,
            media,
            redirects
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json",
            $"wapdabills-export-{DateTime.UtcNow:yyyyMMddHHmm}.json");
    }

    [HttpGet("/admin/import")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult Import()
    {
        ViewData["Title"] = "Import JSON";
        return View("~/Views/Admin/Import.cshtml");
    }

    [HttpPost("/admin/import")]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile? file, bool confirm)
    {
        ViewData["Title"] = "Import JSON";
        if (file is null || file.Length == 0 || file.Length > 20 * 1024 * 1024)
        {
            TempData["Error"] = "Upload a JSON export under 20 MB.";
            return RedirectToAction(nameof(Import));
        }

        if (!confirm)
        {
            TempData["Error"] = "Confirm the import checkbox after reviewing a backup.";
            return RedirectToAction(nameof(Import));
        }

        await using var stream = file.OpenReadStream();
        using var doc = await JsonDocument.ParseAsync(stream);
        // Safe restore: only upsert content pages + faqs + social by simple replace-if-empty strategy is too weak.
        // Full restore replaces published content tables (not AdminUsers / AuditLogs / ContactMessages).
        if (doc.RootElement.TryGetProperty("contentPages", out var pagesEl))
        {
            var pages = JsonSerializer.Deserialize<List<ContentPage>>(pagesEl.GetRawText()) ?? [];
            foreach (var p in pages)
            {
                var existing = await _db.ContentPages.FirstOrDefaultAsync(x => x.Slug == p.Slug);
                if (existing is null)
                {
                    p.Id = 0;
                    _db.ContentPages.Add(p);
                }
                else
                {
                    existing.Title = p.Title;
                    existing.H1 = p.H1;
                    existing.BodyHtml = HtmlSanitize.Clean(p.BodyHtml);
                    existing.IntroHtml = HtmlSanitize.Clean(p.IntroHtml);
                    existing.MetaTitle = p.MetaTitle;
                    existing.MetaDescription = p.MetaDescription;
                    existing.MetaKeywords = p.MetaKeywords;
                    existing.IsPublished = p.IsPublished;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        await _db.SaveChangesAsync();
        _cms.Invalidate();
        await AuditAsync("import", "ContentPages", null, file.FileName);
        TempData["Ok"] = "Import applied for content pages (other entities preserved). Re-seed or edit remaining modules as needed.";
        return RedirectToAction(nameof(Import));
    }

    private async Task EnsureRedirectAsync(string fromPath, string toUrl)
    {
        fromPath = NormalizePath(fromPath);
        if (await _db.RedirectRules.AnyAsync(r => r.FromPath == fromPath))
        {
            return;
        }

        _db.RedirectRules.Add(new RedirectRule
        {
            FromPath = fromPath,
            ToUrl = toUrl.StartsWith('/') || toUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? toUrl : "/" + toUrl,
            StatusCode = 301,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static string NormalizePath(string? path)
    {
        path = (path ?? "").Trim();
        if (string.IsNullOrWhiteSpace(path)) return "";
        if (!path.StartsWith('/')) path = "/" + path;
        var normalized = path.TrimEnd('/');
        return string.IsNullOrEmpty(normalized) ? "/" : normalized.ToLowerInvariant();
    }

    private static string? SanitizeToken(string? value)
    {
        value = value?.Trim();
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Length > 120 || value.Contains('<') || value.Contains('>') ? null : value;
    }

    private static string? SanitizeGa4(string? value)
    {
        value = value?.Trim();
        if (string.IsNullOrWhiteSpace(value)) return null;
        return System.Text.RegularExpressions.Regex.IsMatch(value, @"^G-[A-Z0-9]+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            ? value.ToUpperInvariant()
            : null;
    }

    private static string? SanitizeGtm(string? value)
    {
        value = value?.Trim();
        if (string.IsNullOrWhiteSpace(value)) return null;
        return System.Text.RegularExpressions.Regex.IsMatch(value, @"^GTM-[A-Z0-9]+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            ? value.ToUpperInvariant()
            : null;
    }
}
