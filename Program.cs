using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using WapdaBill.Data;
using WapdaBill.Models.Cms;
using WapdaBill.Services;

var builder = WebApplication.CreateBuilder(args);

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? $"Data Source={Path.Combine(dataDirectory, "wapdabill.db")}";

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<SeoFilter>();
builder.Services.AddScoped<AdminAuthService>();
builder.Services.AddScoped<MediaService>();
builder.Services.AddScoped<CmsQuery>();
builder.Services.AddControllersWithViews(options => options.Filters.AddService<SeoFilter>());
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/admin/login";
        options.AccessDeniedPath = "/admin/login";
        options.Cookie.Name = "wapda.admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("EditorOrAdmin", p => p.RequireRole("Admin", "Editor"));
});
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<PitcBillService>();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHttpClient(PitcOutbound.SnapsClientName, client =>
{
    client.Timeout = PitcOutbound.SnapsTimeout;
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", PitcOutbound.UserAgent);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    AutomaticDecompression = DecompressionMethods.All,
    ConnectTimeout = TimeSpan.FromSeconds(15)
});

builder.Services.AddHttpClient(PitcOutbound.PortalClientName, client =>
{
    client.Timeout = PitcOutbound.PortalTimeout;
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", PitcOutbound.UserAgent);
    client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml");
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    AutomaticDecompression = DecompressionMethods.All,
    AllowAutoRedirect = true,
    ConnectTimeout = TimeSpan.FromSeconds(15)
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var auth = scope.ServiceProvider.GetRequiredService<AdminAuthService>();
    await db.Database.MigrateAsync();
    await ContentSeeder.SeedAsync(db);
    await CmsSeeder.SeedCmsAsync(db);
    await auth.EnsureBootstrapAdminAsync();
}

app.UseForwardedHeaders();
app.UseResponseCompression();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "/";

    if (path.Length > 1 && path.EndsWith('/'))
    {
        var qs = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "";
        context.Response.Redirect(path.TrimEnd('/') + qs, permanent: true);
        return;
    }

    var publicOrigin = SiteUrls.PublicOrigin(app.Configuration);
    if (Uri.TryCreate(publicOrigin, UriKind.Absolute, out var publicUri)
        && !app.Environment.IsDevelopment()
        && !string.Equals(context.Request.Host.Host, publicUri.Host, StringComparison.OrdinalIgnoreCase))
    {
        var target = publicUri.Scheme + "://" + publicUri.Host + path
                     + (context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "");
        context.Response.Redirect(target, permanent: true);
        return;
    }

    // CMS redirect rules (skip admin/static).
    if (!path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)
        && !path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
        && !path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
        && !path.StartsWith("/media", StringComparison.OrdinalIgnoreCase)
        && !path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            var cms = context.RequestServices.GetService<CmsQuery>();
            if (cms is not null)
            {
                var rules = await cms.GetActiveRedirectsAsync();
                var match = rules.FirstOrDefault(r =>
                    string.Equals(r.FromPath.TrimEnd('/'), path.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
                if (match is not null && !string.IsNullOrWhiteSpace(match.ToUrl))
                {
                    context.Response.Redirect(match.ToUrl, permanent: match.StatusCode == 301);
                    return;
                }
            }
        }
        catch
        {
            // Redirect table may not exist yet during first boot; ignore.
        }
    }

    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
        if (path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        }

        return Task.CompletedTask;
    });

    await next();

    if (context.Response.StatusCode == 404
        && !path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)
        && !path.StartsWith("/media", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            var db = context.RequestServices.GetService<AppDbContext>();
            if (db is not null)
            {
                db.NotFoundLogs.Add(new NotFoundLog
                {
                    Path = path.Length > 500 ? path[..500] : path,
                    OccurredAt = DateTime.UtcNow,
                    UserAgent = Truncate(context.Request.Headers.UserAgent.ToString(), 300),
                    Referrer = Truncate(context.Request.Headers.Referer.ToString(), 300)
                });
                await db.SaveChangesAsync();
            }
        }
        catch
        {
            // Logging must never break the response.
        }
    }
});

app.UseStatusCodePagesWithReExecute("/Home/StatusCodePage", "?code={0}");
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (app.Environment.IsDevelopment())
        {
            return;
        }

        ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
    }
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/careers", () => Results.Redirect("/", permanent: true));
app.MapGet("/careers/{id:int}", () => Results.Redirect("/", permanent: true));

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static string? Truncate(string? value, int max) =>
    string.IsNullOrEmpty(value) ? value : value.Length <= max ? value : value[..max];
