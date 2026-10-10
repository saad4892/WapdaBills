using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using WapdaBill.Models;

namespace WapdaBill.Services;

public sealed class PitcBillService
{
    private const string Portal = "https://bill.pitc.com.pk";
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(15);

    private readonly IMemoryCache _cache;
    private readonly ILogger<PitcBillService> _logger;

    public PitcBillService(IMemoryCache cache, ILogger<PitcBillService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<BillFetchResult> GetAsync(
        DiscoCompany company,
        string number,
        string searchBy,
        string ru,
        CancellationToken cancellationToken)
    {
        var key = $"{company.Code}|{searchBy}|{number}|{ru}";
        if (_cache.TryGetValue(key, out BillFetchResult? cached) && cached is { Success: true })
        {
            return cached;
        }

        var result = await FetchFreshAsync(company, number, searchBy, ru, cancellationToken);
        if (result.Success)
        {
            _cache.Set(result.CacheKey, result, CacheFor);
            if (!string.Equals(key, result.CacheKey, StringComparison.Ordinal))
            {
                _cache.Set(key, result, CacheFor);
            }
        }

        return result;
    }

    private async Task<BillFetchResult> FetchFreshAsync(
        DiscoCompany company,
        string number,
        string searchBy,
        string ru,
        CancellationToken cancellationToken)
    {
        try
        {
            var html = await RequestBillAsync(company, number, ru, searchBy, postbackRadio: false, cancellationToken);
            var usedRu = ru;

            if (!IsBill(html, number) && searchBy == "appno")
            {
                html = await RequestBillAsync(company, number, ru, searchBy, postbackRadio: true, cancellationToken);
            }

            if (!IsBill(html, number) && searchBy == "refno" && ru.Length == 0)
            {
                html = await RequestBillAsync(company, number, "R", searchBy, postbackRadio: false, cancellationToken);
                usedRu = "R";
            }

            if (!IsBill(html, number))
            {
                return Fail(company, number, searchBy, ru, NotFoundMessage(company, searchBy));
            }

            var summary = ParseSummary(html);
            return new BillFetchResult
            {
                Success = true,
                DisplayHtml = PrepareForDisplay(html),
                Summary = summary,
                RuUsed = usedRu,
                Number = number,
                SearchBy = searchBy,
                Company = company
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Fail(company, number, searchBy, ru, "The PITC portal did not respond in time. Try again.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "PITC request failed for {Company}", company.Code);
            return Fail(company, number, searchBy, ru, "The request did not reach the PITC portal. Check your connection and try again.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "PITC form could not be read for {Company}", company.Code);
            return Fail(company, number, searchBy, ru, ex.Message);
        }
    }

    private static async Task<string> RequestBillAsync(
        DiscoCompany company,
        string number,
        string ru,
        string searchBy,
        bool postbackRadio,
        CancellationToken cancellationToken)
    {
        var pageUrl = $"{Portal}/{company.Slug}";
        using var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = true
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(40) };
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml");

        using var get = await client.GetAsync(pageUrl, cancellationToken);
        var html = await get.Content.ReadAsStringAsync(cancellationToken);
        if (!get.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"PITC did not return the {company.ShortName} page ({(int)get.StatusCode}).");
        }

        if (postbackRadio)
        {
            var radioFields = BuildFields(html, number, ru, "appno", includeButton: false);
            radioFields["__EVENTTARGET"] = "rbSearchByList$1";
            html = await PostFormAsync(client, pageUrl, radioFields, cancellationToken);
        }

        var fields = BuildFields(html, number, ru, searchBy, includeButton: true);
        return await PostFormAsync(client, pageUrl, fields, cancellationToken);
    }

    private static async Task<string> PostFormAsync(
        HttpClient client,
        string pageUrl,
        Dictionary<string, string> fields,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(fields);
        using var response = await client.PostAsync(pageUrl, content, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static Dictionary<string, string> BuildFields(
        string html,
        string number,
        string ru,
        string searchBy,
        bool includeButton)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in Regex.Matches(html, "<input\\b[^>]*>", RegexOptions.IgnoreCase))
        {
            var tag = match.Value;
            var type = Attr(tag, "type");
            if (type.Equals("submit", StringComparison.OrdinalIgnoreCase)
                || type.Equals("radio", StringComparison.OrdinalIgnoreCase)
                || type.Equals("checkbox", StringComparison.OrdinalIgnoreCase)
                || type.Equals("button", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var name = Attr(tag, "name");
            if (name.Length == 0)
            {
                continue;
            }

            fields[name] = Attr(tag, "value");
        }

        if (!fields.TryGetValue("__VIEWSTATE", out var viewState) || viewState.Length < 20)
        {
            throw new InvalidOperationException("The company bill form could not be read. Try again in a moment.");
        }

        fields["__EVENTTARGET"] = "";
        fields["__EVENTARGUMENT"] = "";
        fields["__LASTFOCUS"] = "";
        fields["rbSearchByList"] = searchBy;
        fields["searchTextBox"] = number;
        fields["ruCodeTextBox"] = searchBy == "refno" ? ru : "";
        if (includeButton)
        {
            fields["btnSearch"] = "Search";
        }

        return fields;
    }

    private static bool IsBill(string html, string number) =>
        html.Contains("payable-card-amount", StringComparison.OrdinalIgnoreCase)
        && html.Contains(number, StringComparison.Ordinal);

    private static BillSummary ParseSummary(string html)
    {
        return new BillSummary
        {
            Name = TextAfter(html, @"class=""val-space val-space--address"">\s*<span>(.*?)</span>"),
            Reference = TextAfter(html, @"class=""val-space val-space--ref-no"">\s*([^<]+)"),
            ConsumerId = TextAfter(html, @"CONSUMER ID</span>.*?<div class=""val-space"">\s*([^<]+)"),
            BillMonth = TextAfter(html, @"data-bill-month=""([^""]*)"""),
            DueDate = TextAfter(html, @"class=""right-main-val right-main-val--due"">\s*([^<]+)"),
            PayableWithin = TextAfter(html, @"class=""payable-card-amount"">\s*([^<]+)"),
            PayableAfter = TextAfter(html, @"class=""lp-surcharge-bottom-val"">\s*([^<]+)"),
            Units = TextAfter(html, @"en-lbl"">UNITS</span>.*?<div class=""val-space"">\s*([^<]+)")
        };
    }

    private static string TextAfter(string html, string pattern)
    {
        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? Clean(match.Groups[1].Value) : "";
    }

    private static string PrepareForDisplay(string html)
    {
        html = Regex.Replace(
            html,
            @"(?<attr>\b(?:href|src|action)\s*=\s*)(?<q>[""'])/(?!/)",
            "${attr}${q}" + Portal + "/",
            RegexOptions.IgnoreCase);
        html = Regex.Replace(
            html,
            @"url\(\s*(?<q>['""]?)/(?!/)",
            "url(${q}" + Portal + "/",
            RegexOptions.IgnoreCase);

        const string hook = """
            <script>
            (function () {
              var api = "usersnap.pitc.com.pk/api/SnapsForDuplicateBill/ToDuplicate";
              if (window.fetch) {
                var original = window.fetch.bind(window);
                window.fetch = function (input, init) {
                  var url = typeof input === "string" ? input : (input && input.url) || "";
                  if (String(url).indexOf(api) !== -1) return original("/api/snaps", init);
                  return original(input, init);
                };
              }
              var origOpen = XMLHttpRequest.prototype.open;
              XMLHttpRequest.prototype.open = function (method, url) {
                if (String(url).indexOf(api) !== -1) arguments[1] = "/api/snaps";
                return origOpen.apply(this, arguments);
              };
              function reportHeight() {
                var height = Math.max(document.body.scrollHeight, document.documentElement.scrollHeight);
                parent.postMessage({ source: "wapda-bill", height: height }, "*");
              }
              window.addEventListener("load", function () {
                reportHeight();
                setTimeout(reportHeight, 700);
                setTimeout(reportHeight, 2000);
              });
            })();
            </script>
            """;

        var head = Regex.Match(html, "<head[^>]*>", RegexOptions.IgnoreCase);
        return head.Success
            ? html.Insert(head.Index + head.Length, hook)
            : hook + html;
    }

    private static string Attr(string tag, string name)
    {
        if (tag.Length == 0)
        {
            return "";
        }

        var match = Regex.Match(tag, $@"\b{name}\s*=\s*(""([^""]*)""|'([^']*)')", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return "";
        }

        var raw = match.Groups[2].Success && match.Groups[2].Length > 0
            ? match.Groups[2].Value
            : match.Groups[3].Value;
        return WebUtility.HtmlDecode(raw);
    }

    private static string Clean(string value)
    {
        var text = Regex.Replace(value, "<.*?>", " ");
        text = WebUtility.HtmlDecode(text);
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    private static string NotFoundMessage(DiscoCompany company, string searchBy)
    {
        var kind = searchBy == "appno" ? "customer ID" : "reference number";
        return $"No {kind} was found for {company.ShortName}. Check the number, the U/R code, or the company.";
    }

    private static BillFetchResult Fail(
        DiscoCompany company,
        string number,
        string searchBy,
        string ru,
        string error) =>
        new()
        {
            Success = false,
            Error = error,
            Company = company,
            Number = number,
            SearchBy = searchBy,
            RuUsed = ru
        };

    public static bool TryNormalize(
        string? input,
        string searchBy,
        string? ru,
        out string number,
        out string usedRu,
        out string? error)
    {
        var compact = new string((input ?? "").Where(ch => !char.IsWhiteSpace(ch)).ToArray()).ToUpperInvariant();
        var digits = new string(compact.Where(char.IsDigit).ToArray());
        var letter = compact.Length > 0 && (compact[^1] == 'U' || compact[^1] == 'R')
            ? compact[^1].ToString()
            : "";
        usedRu = string.Equals(ru, "R", StringComparison.OrdinalIgnoreCase) || letter == "R" ? "R" : "";
        number = digits;

        if (searchBy == "appno")
        {
            usedRu = "";
            if (digits.Length is < 8 or > 14)
            {
                error = "A customer ID is 8 to 14 digits.";
                return false;
            }

            error = null;
            return true;
        }

        if (digits.Length != 14)
        {
            error = "A reference number is 14 digits, printed at the top of the bill.";
            return false;
        }

        error = null;
        return true;
    }
}
