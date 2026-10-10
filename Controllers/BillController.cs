using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using WapdaBill.Models;
using WapdaBill.Services;

namespace WapdaBill.Controllers;

public class BillController : Controller
{
    private static readonly HttpClient SnapsClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private readonly PitcBillService _bills;

    public BillController(PitcBillService bills)
    {
        _bills = bills;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(LookupForm form, CancellationToken cancellationToken)
    {
        var company = DiscoCatalog.Find(form.Company);
        if (company is null)
        {
            form.Error = "Select a region and a company first.";
            return View("~/Views/Home/Index.cshtml", form);
        }

        if (!company.Regions.Contains(form.Region))
        {
            form.Region = company.Regions[0];
        }

        if (!company.UsesPitc)
        {
            return View("Ke", company);
        }

        var searchBy = form.SearchBy == "appno" ? "appno" : "refno";
        if (!PitcBillService.TryNormalize(form.Number, searchBy, form.Ru, out var number, out var ru, out var error))
        {
            form.Error = error;
            form.SearchBy = searchBy;
            return View("~/Views/Home/Index.cshtml", form);
        }

        var result = await _bills.GetAsync(company, number, searchBy, ru, cancellationToken);
        if (!result.Success)
        {
            form.Number = number;
            form.SearchBy = searchBy;
            form.Ru = ru;
            form.Error = result.Error;
            return View("~/Views/Home/Index.cshtml", form);
        }

        return RedirectToAction(nameof(Result), new
        {
            company = company.Code,
            number = result.Number,
            by = result.SearchBy,
            ru = result.RuUsed
        });
    }

    [HttpGet]
    public async Task<IActionResult> Result(
        string company,
        string number,
        string by,
        string? ru,
        CancellationToken cancellationToken)
    {
        var disco = DiscoCatalog.Find(company);
        if (disco is null || !disco.UsesPitc)
        {
            return View("Missing");
        }

        var searchBy = by == "appno" ? "appno" : "refno";
        if (!PitcBillService.TryNormalize(number, searchBy, ru, out var normalized, out var usedRu, out _))
        {
            return View("Missing");
        }

        var result = await _bills.GetAsync(disco, normalized, searchBy, usedRu, cancellationToken);
        if (!result.Success)
        {
            ViewData["Message"] = result.Error;
            return View("Missing");
        }

        Response.Headers.CacheControl = "no-store";
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Document(
        string company,
        string number,
        string by,
        string? ru,
        CancellationToken cancellationToken)
    {
        var disco = DiscoCatalog.Find(company);
        if (disco is null || !disco.UsesPitc)
        {
            return Content("<p>Bill not found.</p>", "text/html");
        }

        var searchBy = by == "appno" ? "appno" : "refno";
        if (!PitcBillService.TryNormalize(number, searchBy, ru, out var normalized, out var usedRu, out _))
        {
            return Content("<p>Bill not found.</p>", "text/html");
        }

        var result = await _bills.GetAsync(disco, normalized, searchBy, usedRu, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        if (!result.Success)
        {
            return Content($"<p>{System.Net.WebUtility.HtmlEncode(result.Error)}</p>", "text/html");
        }

        return Content(result.DisplayHtml, "text/html; charset=utf-8");
    }

    [HttpPost("/api/snaps")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Snaps(CancellationToken cancellationToken)
    {
        string raw;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            raw = await reader.ReadToEndAsync(cancellationToken);
        }

        if (raw.Length is 0 or > 400)
        {
            return BadRequest();
        }

        string refNo;
        string billMonth;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            refNo = doc.RootElement.TryGetProperty("REF_NO", out var refNode) ? refNode.GetString() ?? "" : "";
            billMonth = doc.RootElement.TryGetProperty("BILL_MONTH", out var monthNode) ? monthNode.GetString() ?? "" : "";
        }
        catch (JsonException)
        {
            return BadRequest();
        }

        if (refNo.Length is < 10 or > 16 || billMonth.Length is < 6 or > 20)
        {
            return BadRequest();
        }

        var payload = JsonSerializer.Serialize(new { REF_NO = refNo, BILL_MONTH = billMonth });
        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            "https://usersnap.pitc.com.pk/api/SnapsForDuplicateBill/ToDuplicate")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        message.Headers.Accept.ParseAdd("application/json");

        using var response = await SnapsClient.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = body,
            ContentType = "application/json"
        };
    }
}
