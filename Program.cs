using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WapdaBill.Data;
using WapdaBill.Services;

var builder = WebApplication.CreateBuilder(args);

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? $"Data Source={Path.Combine(dataDirectory, "wapdabill.db")}";

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<SeoFilter>();
builder.Services.AddControllersWithViews(options => options.Filters.AddService<SeoFilter>());
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/admin/login";
        options.AccessDeniedPath = "/admin/login";
        options.Cookie.Name = "wapda.admin";
        options.Cookie.HttpOnly = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<PitcBillService>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "ContactMessages" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_ContactMessages" PRIMARY KEY AUTOINCREMENT,
            "Name" TEXT NOT NULL,
            "Email" TEXT NOT NULL,
            "Message" TEXT NOT NULL,
            "CreatedAt" TEXT NOT NULL,
            "UserAgent" TEXT NULL
        );
        """);
    await ContentSeeder.SeedAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
