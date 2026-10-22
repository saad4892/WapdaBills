using Microsoft.AspNetCore.Mvc;
using WapdaBill.Services;

namespace WapdaBill.Controllers;

public class InfoController : Controller
{
    [HttpGet("/find-number")]
    public IActionResult FindNumber()
    {
        ViewData["Title"] = "Find Electricity Reference Number | WAPDA";
        ViewData["Description"] = "Find the 14-digit reference number, customer ID, and U or R letter on a Pakistan electricity bill.";
        ViewData["Keywords"] = "find reference number, electricity bill reference, customer ID, U R code";
        return View();
    }

    [HttpGet("/resources")]
    public IActionResult Resources()
    {
        ViewData["Title"] = "Electricity Bill Resources | WAPDA Bills";
        ViewData["Description"] = "Bill check tools, reference-number help, company coverage, FAQs, and DISCO office links for Pakistan.";
        ViewData["Keywords"] = "electricity bill resources, LESCO, MEPCO, FESCO, IESCO, PESCO, QESCO";
        return View(SiteContent.Resources);
    }

    [HttpGet("/faqs")]
    public async Task<IActionResult> Faqs([FromServices] CmsQuery cms)
    {
        ViewData["Title"] = "Electricity Bill FAQs Pakistan | WAPDA";
        ViewData["Description"] = "Answers on reference numbers, U/R codes, K-Electric, duplicate bills, and DISCO offices in Pakistan.";
        ViewData["Keywords"] = "electricity bill FAQ, duplicate bill, reference number, WAPDA bill";

        var cmsFaqs = await cms.GetFaqsAsync("global");
        if (cmsFaqs.Count > 0)
        {
            var items = cmsFaqs
                .Select(f => new FaqItem(
                    f.Question,
                    System.Text.RegularExpressions.Regex.Replace(f.AnswerHtml ?? "", "<.*?>", " ").Trim()))
                .ToList();
            return View(items);
        }

        return View(SiteContent.Faqs);
    }

    [HttpGet("/offices")]
    public IActionResult Offices()
    {
        ViewData["Title"] = "DISCO Offices Pakistan | WAPDA Bills";
        ViewData["Description"] = "Headquarters city and service area for LESCO, GEPCO, FESCO, IESCO, MEPCO, PESCO, HAZECO, HESCO, SEPCO, QESCO, TESCO, and K-Electric.";
        ViewData["Keywords"] = "LESCO office, MEPCO office, IESCO office, FESCO office, electricity company headquarters";
        return View(DiscoCatalog.All);
    }
}
