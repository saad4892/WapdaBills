using Microsoft.AspNetCore.Mvc;
using WapdaBill.Services;

namespace WapdaBill.Controllers;

public class InfoController : Controller
{
    [HttpGet("/find-number")]
    public IActionResult FindNumber()
    {
        ViewData["Title"] = "Find your electricity reference number";
        ViewData["Description"] = "Find the 14-digit reference number, customer ID, and U or R letter on a Pakistan electricity bill.";
        ViewData["Keywords"] = "find reference number, electricity bill reference, customer ID, U R code";
        return View();
    }

    [HttpGet("/resources")]
    public IActionResult Resources()
    {
        ViewData["Title"] = "Electricity bill resources";
        ViewData["Description"] = "Bill check, reference-number help, company coverage, FAQs, offices, and career notices for Pakistan distribution companies.";
        ViewData["Keywords"] = "electricity bill resources, LESCO, MEPCO, FESCO, IESCO, PESCO, QESCO";
        return View(SiteContent.Resources);
    }

    [HttpGet("/faqs")]
    public IActionResult Faqs()
    {
        ViewData["Title"] = "Electricity bill FAQs";
        ViewData["Description"] = "Answers on reference numbers, the U and R code, K-Electric, duplicate bills, and distribution company offices.";
        ViewData["Keywords"] = "electricity bill FAQ, duplicate bill, reference number, WAPDA bill";
        return View(SiteContent.Faqs);
    }

    [HttpGet("/offices")]
    public IActionResult Offices()
    {
        ViewData["Title"] = "Distribution company offices";
        ViewData["Description"] = "Headquarters city and service area for LESCO, GEPCO, FESCO, IESCO, MEPCO, PESCO, HAZECO, HESCO, SEPCO, QESCO, TESCO, and K-Electric.";
        ViewData["Keywords"] = "LESCO office, MEPCO office, IESCO office, FESCO office, electricity company headquarters";
        return View(DiscoCatalog.All);
    }
}
