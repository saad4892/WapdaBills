using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using WapdaBill.Data;
using WapdaBill.Models.Cms;

namespace WapdaBill.Services;

public sealed class MediaService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg"
    };

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public MediaService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<MediaAsset?> SaveUploadAsync(IFormFile file, string? altText, CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0 || file.Length > 5 * 1024 * 1024)
        {
            return null;
        }

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
        {
            return null;
        }

        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        if (read < 4 || !LooksLikeImage(header, ext))
        {
            return null;
        }

        stream.Position = 0;
        var folder = Path.Combine(_env.WebRootPath, "media");
        Directory.CreateDirectory(folder);
        var safeName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var physical = Path.Combine(folder, safeName);
        await using (var fs = File.Create(physical))
        {
            await stream.CopyToAsync(fs, cancellationToken);
        }

        var asset = new MediaAsset
        {
            FileName = Path.GetFileName(file.FileName),
            StoragePath = "/media/" + safeName,
            ContentType = file.ContentType,
            ByteSize = file.Length,
            AltText = altText?.Trim() ?? "",
            CreatedAt = DateTime.UtcNow
        };
        _db.MediaAssets.Add(asset);
        await _db.SaveChangesAsync(cancellationToken);
        return asset;
    }

    public async Task<bool> TryDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var asset = await _db.MediaAssets.FindAsync([id], cancellationToken);
        if (asset is null)
        {
            return false;
        }

        var referenced =
            await _db.BlogPosts.AnyAsync(p => p.ImagePath == asset.StoragePath, cancellationToken)
            || await _db.SiteSettings.AnyAsync(s =>
                s.LogoMediaId == id || s.FaviconMediaId == id || s.DefaultOgImageMediaId == id, cancellationToken)
            || await _db.ContentPages.AnyAsync(p => p.OgImageMediaId == id, cancellationToken)
            || await _db.Discos.AnyAsync(d => d.LogoMediaId == id, cancellationToken);
        if (referenced)
        {
            return false;
        }

        var physical = Path.Combine(_env.WebRootPath, asset.StoragePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(physical))
        {
            File.Delete(physical);
        }

        _db.MediaAssets.Remove(asset);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static bool LooksLikeImage(byte[] header, string ext)
    {
        if (ext.Equals(".svg", StringComparison.OrdinalIgnoreCase))
        {
            var text = System.Text.Encoding.UTF8.GetString(header);
            return text.Contains('<') || text.Contains("svg", StringComparison.OrdinalIgnoreCase);
        }

        // JPEG
        if (header[0] == 0xFF && header[1] == 0xD8)
        {
            return true;
        }

        // PNG
        if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
        {
            return true;
        }

        // GIF
        if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46)
        {
            return true;
        }

        // WEBP (RIFF....WEBP)
        if (header.Length >= 12
            && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
            && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
        {
            return true;
        }

        return false;
    }
}
