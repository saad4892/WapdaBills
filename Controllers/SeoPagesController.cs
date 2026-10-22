using Microsoft.AspNetCore.Mvc;
using WapdaBill.Data;
using WapdaBill.Models;
using WapdaBill.Models.Cms;
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
    public async Task<IActionResult> DiscoLanding([FromServices] CmsQuery cms)
    {
        var link = SiteNav.FindDiscoByPath(Request.Path);
        var cmsDisco = await cms.FindDiscoByPathAsync(Request.Path);
        if (link is null && cmsDisco is null)
        {
            return NotFound();
        }

        DiscoCompany? company = null;
        var code = link?.CompanyCode ?? cmsDisco?.Code;
        if (!string.IsNullOrWhiteSpace(code))
        {
            company = DiscoCatalog.Find(code);
        }

        ViewData["Title"] = !string.IsNullOrWhiteSpace(cmsDisco?.MetaTitle)
            ? cmsDisco!.MetaTitle
            : $"{link?.Title ?? cmsDisco!.ShortName} Online | WAPDA Bills";
        ViewData["Description"] = !string.IsNullOrWhiteSpace(cmsDisco?.MetaDescription)
            ? cmsDisco!.MetaDescription
            : company is null
                ? $"Check {link?.Title} online. {link?.Description}."
                : $"Check {company.ShortName} bill online for {company.Coverage}. Free live duplicate for {company.FullName} consumers.";
        ViewData["Keywords"] = !string.IsNullOrWhiteSpace(cmsDisco?.MetaKeywords)
            ? cmsDisco!.MetaKeywords
            : $"{company?.ShortName ?? link?.Title} bill online";
        ViewData["NavLink"] = link ?? new NavLink(cmsDisco!.ShortName + " Bill Online", "/" + cmsDisco.PublicSlug, cmsDisco.Coverage, cmsDisco.Code);
        ViewData["Company"] = company;
        ViewData["CmsDisco"] = cmsDisco;
        ViewData["CheckerUrl"] = SiteNav.CheckerUrl(code, link?.RegionKey ?? company?.Regions.FirstOrDefault());
        return View("Disco");
    }

    [HttpGet("/punjab-electricity-bills")]
    [HttpGet("/sindh-electricity-bills")]
    [HttpGet("/kpk-electricity-bills")]
    [HttpGet("/balochistan-electricity-bills")]
    [HttpGet("/kashmir-gb-electricity-bills")]
    public async Task<IActionResult> RegionLanding([FromServices] CmsQuery cms)
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

        var cmsHub = await cms.FindRegionByPathAsync(Request.Path);
        if (cmsHub is not null)
        {
            ViewData["Title"] = (!string.IsNullOrWhiteSpace(cmsHub.MetaTitle) ? cmsHub.MetaTitle : hub.MetaTitle) + " | WAPDA Bills";
            ViewData["Description"] = !string.IsNullOrWhiteSpace(cmsHub.MetaDescription) ? cmsHub.MetaDescription : hub.MetaDescription;
            ViewData["Keywords"] = !string.IsNullOrWhiteSpace(cmsHub.MetaKeywords) ? cmsHub.MetaKeywords : hub.MetaKeywords;
            if (!string.IsNullOrWhiteSpace(cmsHub.H1))
            {
                hub = hub with { H1 = cmsHub.H1, Label = string.IsNullOrWhiteSpace(cmsHub.Label) ? hub.Label : cmsHub.Label };
            }

            ViewBag.CmsIntroHtml = cmsHub.IntroHtml;
            ViewBag.CmsBodyHtml = cmsHub.BodyHtml;
        }
        else
        {
            ViewData["Title"] = hub.MetaTitle + " | WAPDA Bills";
            ViewData["Description"] = hub.MetaDescription;
            ViewData["Keywords"] = hub.MetaKeywords;
        }

        var companies = string.IsNullOrWhiteSpace(link.RegionKey)
            ? Array.Empty<DiscoCompany>()
            : DiscoCatalog.All.Where(c => c.Regions.Contains(link.RegionKey)).ToArray();

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
    public Task<IActionResult> BillCalculator([FromServices] CmsQuery cms) =>
        ToolFromCms(cms, "bill-calculator",
            "Electricity Bill Estimator",
            "Estimate your Pakistan electricity bill from units consumed. Use the official DISCO checker for the exact duplicate bill.",
            "electricity bill calculator, unit estimator, Pakistan tariff",
            "Bill Estimator",
            "Enter your units on the official company portal after estimating. This page helps you pick the right DISCO and open the live bill checker.");

    [HttpGet("/peak-hours-guide")]
    public Task<IActionResult> PeakHours([FromServices] CmsQuery cms) =>
        ToolFromCms(cms, "peak-hours-guide",
            "Peak Hours Timing Chart",
            "Peak and off-peak electricity timing guidance for Pakistan distribution companies.",
            "peak hours Pakistan, TOU timing, electricity peak hours",
            "Peak Hours Timing",
            "Peak hours vary by company and season. Confirm the latest TOU schedule on your bill or company notice, then check your bill here.");

    [HttpGet("/unit-rates-tariff")]
    public Task<IActionResult> UnitRates([FromServices] CmsQuery cms) =>
        ToolFromCms(cms, "unit-rates-tariff",
            "Per Unit Electricity Tariff",
            "Overview of per-unit electricity tariff slabs in Pakistan. Always verify the payable amount on your official duplicate bill.",
            "electricity tariff Pakistan, per unit rate, NEPRA slabs",
            "Unit Rates & Tariff",
            "Tariff slabs change with NEPRA notifications. Use this hub to reach your DISCO and open the latest payable amount from the official record.");

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
    public async Task<IActionResult> About([FromServices] CmsQuery cms)
    {
        await ApplyCmsLegal(cms, "about-us", "About Us",
            "WAPDA Bills helps Pakistan consumers open electricity duplicate bills for LESCO, MEPCO, IESCO, FESCO and other DISCOs.",
            "about WAPDA Bills, electricity bill checker Pakistan",
            LegalContent.About);
        return View("About");
    }

    [HttpGet("/about")]
    public IActionResult AboutRedirect() => RedirectToActionPermanent(nameof(About));

    [HttpGet("/privacy-policy")]
    public async Task<IActionResult> Privacy([FromServices] CmsQuery cms)
    {
        await ApplyCmsLegal(cms, "privacy-policy", "Privacy Policy",
            "Privacy policy for WAPDA Bills covering AdSense cookies, logs, analytics, and bill lookup data practices.",
            "privacy policy, AdSense cookies, WAPDA Bills privacy",
            LegalContent.Privacy);
        return View("Privacy");
    }

    [HttpGet("/privacy")]
    public IActionResult PrivacyRedirect() => RedirectToActionPermanent(nameof(Privacy));

    [HttpGet("/terms-and-conditions")]
    public async Task<IActionResult> Terms([FromServices] CmsQuery cms)
    {
        await ApplyCmsLegal(cms, "terms-and-conditions", "Terms & Conditions",
            "Terms and conditions for using the WAPDA Bills electricity bill checker website.",
            "terms and conditions, WAPDA Bills terms",
            LegalContent.Terms);
        return View("Terms");
    }

    [HttpGet("/terms")]
    public IActionResult TermsRedirect() => RedirectToActionPermanent(nameof(Terms));

    [HttpGet("/disclaimer")]
    public async Task<IActionResult> Disclaimer([FromServices] CmsQuery cms)
    {
        await ApplyCmsLegal(cms, "disclaimer", "Legal Disclaimer",
            "Disclaimer: WAPDA Bills is a third-party helper with no official link to WAPDA, NEPRA, or any government DISCO.",
            "legal disclaimer, WAPDA Bills disclaimer, third party",
            LegalContent.Disclaimer);
        return View("Disclaimer");
    }

    [HttpGet("/contact-us")]
    public IActionResult Contact(bool sent = false)
    {
        SetContactMeta();
        ViewData["ContactSent"] = sent;
        if (sent)
        {
            ViewData["Robots"] = "noindex, nofollow";
            ViewData["Title"] = "Message sent | WAPDA Bills";
            ViewData["Description"] = "Your contact message was received. This confirmation page is not indexed.";
        }
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

    private async Task ApplyCmsLegal(
        CmsQuery cms,
        string slug,
        string title,
        string description,
        string keywords,
        IReadOnlyList<LegalSection> fallback)
    {
        var page = await cms.FindPageBySlugAsync(slug);
        if (page is not null)
        {
            ViewData["Title"] = !string.IsNullOrWhiteSpace(page.MetaTitle) ? page.MetaTitle : title + " | WAPDA Bills";
            ViewData["Description"] = !string.IsNullOrWhiteSpace(page.MetaDescription) ? page.MetaDescription : description;
            ViewData["Keywords"] = !string.IsNullOrWhiteSpace(page.MetaKeywords) ? page.MetaKeywords : keywords;
            if (!string.IsNullOrWhiteSpace(page.Robots))
            {
                ViewData["Robots"] = page.Robots;
            }

            ViewBag.CmsH1 = string.IsNullOrWhiteSpace(page.H1) ? title : page.H1;
            ViewBag.CmsBodyHtml = page.BodyHtml;
            ViewBag.LegalFallback = fallback;
        }
        else
        {
            SetMeta(title, description, keywords);
            ViewBag.CmsH1 = title;
            ViewBag.LegalFallback = fallback;
        }
    }

    private async Task<IActionResult> ToolFromCms(
        CmsQuery cms,
        string slug,
        string title,
        string description,
        string keywords,
        string heading,
        string body)
    {
        var page = await cms.FindPageBySlugAsync(slug);
        if (page is not null)
        {
            ViewData["Title"] = !string.IsNullOrWhiteSpace(page.MetaTitle) ? page.MetaTitle : title + " | WAPDA Bills";
            ViewData["Description"] = !string.IsNullOrWhiteSpace(page.MetaDescription) ? page.MetaDescription : description;
            ViewData["Keywords"] = !string.IsNullOrWhiteSpace(page.MetaKeywords) ? page.MetaKeywords : keywords;
            if (!string.IsNullOrWhiteSpace(page.Robots))
            {
                ViewData["Robots"] = page.Robots;
            }

            var intro = StripTags(page.IntroHtml);
            return View("Tool", model: (
                string.IsNullOrWhiteSpace(page.H1) ? heading : page.H1,
                string.IsNullOrWhiteSpace(intro) ? body : intro));
        }

        SetMeta(title, description, keywords);
        return View("Tool", model: (heading, body));
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

    private static string StripTags(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return "";
        }

        return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", " ").Trim();
    }

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? value : value.Length <= max ? value : value[..max];

    private static string EmailHost(string email)
    {
        var at = email.IndexOf('@');
        return at >= 0 && at < email.Length - 1 ? email[(at + 1)..] : "unknown";
    }
}
