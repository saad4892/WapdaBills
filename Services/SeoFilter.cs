using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WapdaBill.Data;

namespace WapdaBill.Services;

public class SeoFilter : IAsyncActionFilter
{
    private readonly AppDbContext _db;

    public SeoFilter(AppDbContext db)
    {
        _db = db;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await next();
        if (context.Controller is not Controller controller)
        {
            return;
        }

        var settings = await _db.SiteSettings.FindAsync(1);
        if (settings is null)
        {
            return;
        }

        controller.ViewData["Title"] ??= settings.SiteTitle;
        controller.ViewData["Description"] ??= settings.MetaDescription;
        controller.ViewData["Keywords"] ??= settings.MetaKeywords;
    }
}
