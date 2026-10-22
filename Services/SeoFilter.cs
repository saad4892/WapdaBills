using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WapdaBill.Services;

public class SeoFilter : IAsyncActionFilter
{
    private readonly CmsQuery _cms;

    public SeoFilter(CmsQuery cms)
    {
        _cms = cms;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await next();
        if (context.Controller is not Controller controller)
        {
            return;
        }

        var settings = await _cms.GetSettingsAsync();
        if (settings is null)
        {
            return;
        }

        controller.ViewData["Title"] ??= settings.SiteTitle;
        controller.ViewData["Description"] ??= settings.MetaDescription;
        controller.ViewData["Keywords"] ??= settings.MetaKeywords;
    }
}
