using Microsoft.AspNetCore.Mvc;
using WapdaBill.Data;
using WapdaBill.Models;
using WapdaBill.Services;

namespace WapdaBill.Controllers;

public class SeoPagesController : Controller
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<SeoPagesController> _logger;

    public SeoPagesController(AppDbContext db, IConfiguration config, ILogger<SeoPagesController> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    [HttpGet("/lesco-bill")]
    [HttpGet("/mepco-bill")]
    [HttpGet("/iesco-bill")]
    [HttpGet("/fesco-bill")]
    [HttpGet("/gepco-bill")]
    [HttpGet("/pesco-bill")]
    [HttpGet("/hazeco-bill")]
    [HttpGet("/tesco-bill")]
    [HttpGet("/kelectric-bill")]
    [HttpGet("/hesco-bill")]
    [HttpGet("/sepco-bill")]
    [HttpGet("/qesco-bill")]
    [HttpGet("/ajked-bill")]
    public IActionResult DiscoLanding()
    {
        var link = SiteNav.FindDiscoByPath(Request.Path);
        if (link is null)
        {
            return NotFound();
        }

        DiscoCompany? company = null;
        if (!string.IsNullOrWhiteSpace(link.CompanyCode))
        {
            company = DiscoCatalog.Find(link.CompanyCode);
        }

        ViewData["Title"] = link.Title + " | WAPDA Bills";
        ViewData["Description"] = company is null
            ? $"{link.Title}. {link.Description}. Check and open your electricity bill online in Pakistan."
            : $"Check {company.ShortName} ({company.FullName}) electricity bills online. Coverage: {company.Coverage}.";
        ViewData["Keywords"] = $"{link.Title}, {company?.ShortName} bill, electricity bill Pakistan, duplicate bill";
        ViewData["NavLink"] = link;
        ViewData["Company"] = company;
        ViewData["CheckerUrl"] = SiteNav.CheckerUrl(link.CompanyCode, link.RegionKey ?? company?.Regions.FirstOrDefault());
        return View("Disco");
    }

    [HttpGet("/punjab-electricity-bills")]
    [HttpGet("/sindh-electricity-bills")]
    [HttpGet("/kpk-electricity-bills")]
    [HttpGet("/balochistan-electricity-bills")]
    [HttpGet("/kashmir-gb-electricity-bills")]
    public IActionResult RegionLanding()
    {
        var link = SiteNav.FindRegionByPath(Request.Path);
        if (link is null)
        {
            return NotFound();
        }

        var hub = RegionalHubContent.FindByPath(Request.Path)
                  ?? RegionalHubContent.All.FirstOrDefault(h =>
                      string.Equals(h.RegionKey, link.RegionKey, StringComparison.OrdinalIgnoreCase));
        if (hub is null)
        {
            return NotFound();
        }

        var companies = string.IsNullOrWhiteSpace(link.RegionKey)
            ? Array.Empty<DiscoCompany>()
            : DiscoCatalog.All.Where(c => c.Regions.Contains(link.RegionKey)).ToArray();

        ViewData["Title"] = hub.MetaTitle + " | WAPDA Bills";
        ViewData["Description"] = hub.MetaDescription;
        ViewData["Keywords"] = hub.MetaKeywords;
        ViewData["NavLink"] = link;
        ViewData["Hub"] = hub;
        ViewData["Companies"] = companies;
        ViewData["Cities"] = RegionalHubContent.GetCities(companies);
        ViewData["CheckerUrl"] = SiteNav.CheckerUrl(null, link.RegionKey);
        ViewData["RelatedRegions"] = SiteNav.RegionLandings
            .Where(r => !string.Equals(r.Href, link.Href, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return View("Region");
    }

    [HttpGet("/bill-calculator")]
    public IActionResult BillCalculator()
    {
        SetMeta(
            "Electricity Bill Estimator",
            "Estimate your Pakistan electricity bill from units consumed. Use the official DISCO checker for the exact duplicate bill.",
            "electricity bill calculator, unit estimator, Pakistan tariff");
        return View("Tool", model: ("Bill Estimator", "Enter your units on the official company portal after estimating. This page helps you pick the right DISCO and open the live bill checker."));
    }

    [HttpGet("/peak-hours-guide")]
    public IActionResult PeakHours()
    {
        SetMeta(
            "Peak Hours Timing Chart",
            "Peak and off-peak electricity timing guidance for Pakistan distribution companies.",
            "peak hours Pakistan, TOU timing, electricity peak hours");
        return View("Tool", model: ("Peak Hours Timing", "Peak hours vary by company and season. Confirm the latest TOU schedule on your bill or company notice, then check your bill here."));
    }

    [HttpGet("/unit-rates-tariff")]
    public IActionResult UnitRates()
    {
        SetMeta(
            "Per Unit Electricity Tariff",
            "Overview of per-unit electricity tariff slabs in Pakistan. Always verify the payable amount on your official duplicate bill.",
            "electricity tariff Pakistan, per unit rate, NEPRA slabs");
        return View("Tool", model: ("Unit Rates & Tariff", "Tariff slabs change with NEPRA notifications. Use this hub to reach your DISCO and open the latest payable amount from the official record."));
    }

    [HttpGet("/how-to-check-bill")]
    public IActionResult HowToCheckBill()
    {
        SetMeta(
            "Find 14-Digit Reference Number",
            "Find the 14-digit reference number, customer ID, and U or R letter on a Pakistan electricity bill.",
            "find reference number, how to check bill, 14 digit reference");
        return View("~/Views/Info/FindNumber.cshtml");
    }

    [HttpGet("/online-bill-payment")]
    public IActionResult OnlinePayment()
    {
        SetMeta(
            "Online Bill Payment Guide",
            "How to pay Pakistan electricity bills online through banks, wallets, and company portals.",
            "online bill payment, LESCO payment, MEPCO payment");
        return View("Guide", model: ("Online Bill Payment", new[]
        {
            "Open your duplicate bill first so you have the correct payable amount and due date.",
            "Pay through your bank app, JazzCash, EasyPaisa, or the company’s published payment channels.",
            "Keep the transaction ID until the bill status updates on the next cycle."
        }));
    }

    [HttpGet("/complaint-helpline")]
    public IActionResult ComplaintHelpline()
    {
        SetMeta(
            "All DISCOs Helpline Numbers",
            "Complaint and helpline starting points for Pakistan electricity distribution companies.",
            "DISCO helpline, electricity complaint, LESCO helpline");
        return View("Guide", model: ("DISCO Helplines", new[]
        {
            "Use the company printed on your bill. Helpline numbers are published by each DISCO and may change.",
            "Have your 14-digit reference number ready before you call.",
            "For office locations and coverage, see the offices page linked below."
        }));
    }

    [HttpGet("/about-us")]
    public IActionResult About()
    {
        SetMeta(
            "About Us",
            "WAPDA Bills helps Pakistan consumers open electricity duplicate bills for LESCO, MEPCO, IESCO, FESCO and other DISCOs.",
            "about WAPDA Bills, electricity bill checker Pakistan");
        return View("About");
    }

    [HttpGet("/about")]
    public IActionResult AboutRedirect() => RedirectToActionPermanent(nameof(About));

    [HttpGet("/privacy-policy")]
    public IActionResult Privacy()
    {
        SetMeta(
            "Privacy Policy",
            "Privacy policy for WAPDA Bills covering AdSense cookies, logs, analytics, and bill lookup data practices.",
            "privacy policy, AdSense cookies, WAPDA Bills privacy");
        return View("Privacy");
    }

    [HttpGet("/privacy")]
    public IActionResult PrivacyRedirect() => RedirectToActionPermanent(nameof(Privacy));

    [HttpGet("/terms-and-conditions")]
    public IActionResult Terms()
    {
        SetMeta(
            "Terms & Conditions",
            "Terms and conditions for using the WAPDA Bills electricity bill checker website.",
            "terms and conditions, WAPDA Bills terms");
        return View("Terms");
    }

    [HttpGet("/terms")]
    public IActionResult TermsRedirect() => RedirectToActionPermanent(nameof(Terms));

    [HttpGet("/disclaimer")]
    public IActionResult Disclaimer()
    {
        SetMeta(
            "Legal Disclaimer",
            "Disclaimer: WAPDA Bills is a third-party helper with no official link to WAPDA, NEPRA, or any government DISCO.",
            "legal disclaimer, WAPDA Bills disclaimer, third party");
        return View("Disclaimer");
    }

    [HttpGet("/contact-us")]
    public IActionResult Contact(bool sent = false)
    {
        SetContactMeta();
        ViewData["ContactSent"] = sent;
        return View(new ContactFormModel());
    }

    [HttpGet("/contact")]
    public IActionResult ContactRedirect() => RedirectToActionPermanent(nameof(Contact));

    [HttpPost("/contact-us")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactFormModel model, CancellationToken cancellationToken)
    {
        SetContactMeta();

        if (!string.IsNullOrWhiteSpace(model.Website))
        {
            _logger.LogInformation("Contact honeypot triggered; message discarded.");
            return RedirectToAction(nameof(Contact), new { sent = true });
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _db.ContactMessages.Add(new ContactMessage
        {
            Name = model.Name.Trim(),
            Email = model.Email.Trim(),
            Message = model.Message.Trim(),
            CreatedAt = DateTime.UtcNow,
            UserAgent = Truncate(Request.Headers.UserAgent.ToString(), 300)
        });
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Contact message stored from {EmailHost}", EmailHost(model.Email));

        return RedirectToAction(nameof(Contact), new { sent = true });
    }

    private void SetContactMeta()
    {
        SetMeta(
            "Contact Us",
            "Contact WAPDA Bills for website feedback, privacy questions, or legal notices.",
            "contact WAPDA Bills, privacy email, electricity bill support");
        ViewData["PublicEmail"] = _config["Contact:PublicEmail"] ?? "privacy@example.com";
        ViewData["PrivacyEmail"] = _config["Contact:PrivacyEmail"] ?? _config["Contact:PublicEmail"] ?? "privacy@example.com";
    }

    private void SetMeta(string title, string description, string keywords)
    {
        ViewData["Title"] = title + " | WAPDA Bills";
        ViewData["Description"] = description;
        ViewData["Keywords"] = keywords;
    }

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? value : value.Length <= max ? value : value[..max];

    private static string EmailHost(string email)
    {
        var at = email.IndexOf('@');
        return at >= 0 && at < email.Length - 1 ? email[(at + 1)..] : "unknown";
    }
}
