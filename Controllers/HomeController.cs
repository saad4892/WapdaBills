using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WapdaBill.Models;
using WapdaBill.Services;

namespace WapdaBill.Controllers;

public class HomeController : Controller
{
    public async Task<IActionResult> Index(string? company, string? region, [FromServices] CmsQuery cms)
    {
        var settings = await cms.GetSettingsAsync();
        ViewData["Title"] = string.IsNullOrWhiteSpace(settings?.SiteTitle)
            ? "Check Electricity Bill Online Pakistan | WAPDA"
            : settings!.SiteTitle;
        ViewData["Description"] = string.IsNullOrWhiteSpace(settings?.MetaDescription)
            ? "Check free live duplicate electricity bills online for LESCO, MEPCO, IESCO, K-Electric, FESCO, PESCO, GEPCO, HESCO, SEPCO and QESCO in Pakistan."
            : settings!.MetaDescription;
        ViewData["Keywords"] = string.IsNullOrWhiteSpace(settings?.MetaKeywords)
            ? "check electricity bill online Pakistan, LESCO bill, MEPCO bill, IESCO bill, FESCO bill, K-Electric bill, duplicate bill, reference number"
            : settings!.MetaKeywords;

        var form = new LookupForm();
        var disco = DiscoCatalog.Find(company);
        if (disco is not null)
        {
            form.Company = disco.Code;
            form.Region = !string.IsNullOrWhiteSpace(region) && disco.Regions.Contains(region)
                ? region
                : disco.Regions[0];
        }
        else if (!string.IsNullOrWhiteSpace(region)
                 && DiscoCatalog.Regions.Any(r => r.Key == region))
        {
            form.Region = region;
        }

        return View(form);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewData["Title"] = "Something went wrong | WAPDA Bills";
        ViewData["Description"] = "An unexpected error occurred while loading this page.";
        ViewData["Robots"] = "noindex, nofollow";
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet("/Home/StatusCodePage")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusCodePage(int code = 404)
    {
        ViewData["Title"] = code == 404 ? "Page not found | WAPDA Bills" : "Error | WAPDA Bills";
        ViewData["Description"] = code == 404
            ? "The page you requested was not found. Return home to check an electricity bill online."
            : "The requested page could not be loaded.";
        ViewData["Robots"] = "noindex, nofollow";
        ViewData["StatusCode"] = code;
        Response.StatusCode = code;
        return View("NotFound");
    }
}
