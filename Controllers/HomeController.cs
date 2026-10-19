using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WapdaBill.Models;
using WapdaBill.Services;

namespace WapdaBill.Controllers;

public class HomeController : Controller
{
    public IActionResult Index(string? company, string? region)
    {
        ViewData["Title"] = "Check Electricity Bill Online in Pakistan | WAPDA Bills";
        ViewData["Description"] =
            "Check and view free live duplicate electricity bills online for LESCO, MEPCO, IESCO, K-Electric, FESCO, PESCO, GEPCO, HESCO, SEPCO, and QESCO in Pakistan.";
        ViewData["Keywords"] =
            "check electricity bill online Pakistan, LESCO bill, MEPCO bill, IESCO bill, FESCO bill, K-Electric bill, duplicate bill, reference number";

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
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
