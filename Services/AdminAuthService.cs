using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WapdaBill.Data;
using WapdaBill.Models.Cms;

namespace WapdaBill.Services;

public sealed class AdminAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;
    private readonly PasswordHasher<AdminUser> _hasher = new();

    public AdminAuthService(AppDbContext db, IConfiguration config, IMemoryCache cache)
    {
        _db = db;
        _config = config;
        _cache = cache;
    }

    public async Task EnsureBootstrapAdminAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.AdminUsers.AnyAsync(cancellationToken))
        {
            return;
        }

        var username = _config["Admin:Username"] ?? "admin";
        var password = _config["Admin:BootstrapPassword"]
                       ?? _config["Admin:Password"]
                       ?? "";
        if (string.IsNullOrWhiteSpace(password))
        {
            password = "ChangeMeNow!" + Guid.NewGuid().ToString("N")[..8];
        }

        var user = new AdminUser
        {
            Username = username.Trim(),
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, password);
        _db.AdminUsers.Add(user);
        _db.AuditLogs.Add(new AuditLog
        {
            Username = "system",
            Action = "bootstrap",
            EntityType = "AdminUser",
            EntityId = username,
            Detail = "Initial admin user created from configuration."
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(bool Ok, string? Error)> TrySignInAsync(
        HttpContext http,
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        username = (username ?? "").Trim();
        var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var throttleKey = "admin-login:" + ip;
        if (_cache.TryGetValue(throttleKey, out int attempts) && attempts >= 20)
        {
            return (false, "Too many login attempts. Try again in a few minutes.");
        }

        _cache.Set(throttleKey, attempts + 1, TimeSpan.FromMinutes(15));

        var user = await _db.AdminUsers.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        if (user is null)
        {
            return (false, "Those details do not match.");
        }

        if (user.LockoutEndUtc is { } lockout && lockout > DateTime.UtcNow)
        {
            return (false, "Account temporarily locked. Try again later.");
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password ?? "");
        if (result == PasswordVerificationResult.Failed)
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= 8)
            {
                user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(20);
                user.FailedLoginCount = 0;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return (false, "Those details do not match.");
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, password!);
        }

        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("uid", user.Id.ToString())
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return (true, null);
    }
}
