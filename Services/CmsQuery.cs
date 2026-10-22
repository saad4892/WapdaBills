using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WapdaBill.Data;
using WapdaBill.Models;
using WapdaBill.Models.Cms;

namespace WapdaBill.Services;

/// <summary>Cached CMS reads with graceful empty fallbacks for public rendering.</summary>
public sealed class CmsQuery
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public CmsQuery(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public void Invalidate()
    {
        _cache.Remove("cms:settings");
        _cache.Remove("cms:nav");
        _cache.Remove("cms:footer");
        _cache.Remove("cms:social");
        _cache.Remove("cms:footer-copy");
        _cache.Remove("cms:discos");
        _cache.Remove("cms:home-sections");
        _cache.Remove("cms:faqs-home");
        _cache.Remove("cms:faqs-global");
    }

    public Task<SiteSetting?> GetSettingsAsync(CancellationToken ct = default) =>
        _cache.GetOrCreateAsync("cms:settings", async e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            return await _db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1, ct);
        })!;

    public Task<List<NavMenu>> GetNavMenusAsync(CancellationToken ct = default) =>
        _cache.GetOrCreateAsync("cms:nav", async e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            return await _db.NavMenus.AsNoTracking()
                .Include(m => m.Items.Where(i => i.IsVisible).OrderBy(i => i.SortOrder))
                .Where(m => m.IsVisible)
                .OrderBy(m => m.SortOrder)
                .ToListAsync(ct);
        })!;

    public Task<List<FooterColumnEntity>> GetFooterColumnsAsync(CancellationToken ct = default) =>
        _cache.GetOrCreateAsync("cms:footer", async e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            return await _db.FooterColumns.AsNoTracking()
                .Include(c => c.Links.Where(l => l.IsVisible).OrderBy(l => l.SortOrder))
                .Where(c => c.IsVisible)
                .OrderBy(c => c.SortOrder)
                .ToListAsync(ct);
        })!;

    public Task<List<SocialLinkEntity>> GetSocialLinksAsync(CancellationToken ct = default) =>
        _cache.GetOrCreateAsync("cms:social", async e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            return await _db.SocialLinks.AsNoTracking()
                .Where(s => s.IsVisible)
                .OrderBy(s => s.SortOrder)
                .ToListAsync(ct);
        })!;

    public Task<FooterCopy?> GetFooterCopyAsync(CancellationToken ct = default) =>
        _cache.GetOrCreateAsync("cms:footer-copy", async e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            return await _db.FooterCopies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == 1, ct);
        })!;

    public Task<List<DiscoEntity>> GetPublishedDiscosAsync(CancellationToken ct = default) =>
        _cache.GetOrCreateAsync("cms:discos", async e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            return await _db.Discos.AsNoTracking()
                .Where(d => d.IsPublished)
                .OrderBy(d => d.SortOrder)
                .ToListAsync(ct);
        })!;

    public Task<DiscoEntity?> FindDiscoByPathAsync(string path, CancellationToken ct = default)
    {
        var slug = path.Trim('/').ToLowerInvariant();
        return _db.Discos.AsNoTracking()
            .FirstOrDefaultAsync(d => d.IsPublished && d.PublicSlug == slug, ct);
    }

    public Task<RegionHubEntity?> FindRegionByPathAsync(string path, CancellationToken ct = default)
    {
        var slug = path.Trim('/').ToLowerInvariant();
        return _db.RegionHubs.AsNoTracking()
            .FirstOrDefaultAsync(r => r.IsPublished && r.Path == slug, ct);
    }

    public Task<ContentPage?> FindPageBySlugAsync(string slug, CancellationToken ct = default) =>
        _db.ContentPages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IsPublished && p.Slug == slug, ct);

    public Task<List<HomepageSection>> GetHomeSectionsAsync(CancellationToken ct = default) =>
        _cache.GetOrCreateAsync("cms:home-sections", async e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            return await _db.HomepageSections.AsNoTracking()
                .Include(s => s.Items.Where(i => i.IsVisible).OrderBy(i => i.SortOrder))
                .Where(s => s.IsVisible)
                .OrderBy(s => s.SortOrder)
                .ToListAsync(ct);
        })!;

    public Task<List<FaqEntity>> GetFaqsAsync(string scope, CancellationToken ct = default)
    {
        var key = "cms:faqs-" + scope;
        return _cache.GetOrCreateAsync(key, async e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            return await _db.Faqs.AsNoTracking()
                .Where(f => f.IsPublished && f.Scope == scope)
                .OrderBy(f => f.SortOrder)
                .ToListAsync(ct);
        })!;
    }

    public Task<List<RedirectRule>> GetActiveRedirectsAsync(CancellationToken ct = default) =>
        _db.RedirectRules.AsNoTracking()
            .Where(r => r.IsActive)
            .ToListAsync(ct);
}
