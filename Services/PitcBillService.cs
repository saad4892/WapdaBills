using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using WapdaBill.Models;

namespace WapdaBill.Services;

public sealed class PitcBillService
{
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
        var sw = Stopwatch.StartNew();
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
                _logger.LogInformation(
                    "PITC bill not found. Company={Company} SearchBy={SearchBy} NumberLen={NumberLen} ElapsedMs={ElapsedMs}",
                    company.Code,
                    searchBy,
                    number.Length,
                    sw.ElapsedMilliseconds);
                return Fail(company, number, searchBy, ru, NotFoundMessage(company, searchBy));
            }

            var summary = ParseSummary(html);
            _logger.LogInformation(
                "PITC bill fetched. Company={Company} SearchBy={SearchBy} NumberLen={NumberLen} ElapsedMs={ElapsedMs}",
                company.Code,
                searchBy,
                number.Length,
                sw.ElapsedMilliseconds);
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
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            PitcHttpLogging.LogOutboundFailure(
                _logger, ex, "bill_fetch", "bill.pitc.com.pk", company.Slug, company.Code,
                sw.ElapsedMilliseconds, statusCode: null, responseBodyPreview: null, requestAborted: true);
            return Fail(company, number, searchBy, ru, "The request was cancelled. Try again.");
        }
        catch (OperationCanceledException ex)
        {
            PitcHttpLogging.LogOutboundFailure(
                _logger, ex, "bill_fetch", "bill.pitc.com.pk", company.Slug, company.Code,
                sw.ElapsedMilliseconds, statusCode: null, responseBodyPreview: null, requestAborted: false);
            return Fail(company, number, searchBy, ru, "The PITC portal did not respond in time. Try again.");
        }
        catch (HttpRequestException ex)
        {
            PitcHttpLogging.LogOutboundFailure(
                _logger, ex, "bill_fetch", "bill.pitc.com.pk", company.Slug, company.Code,
                sw.ElapsedMilliseconds, ex.StatusCode, responseBodyPreview: null, requestAborted: false);
            return Fail(company, number, searchBy, ru, "The request did not reach the PITC portal. Check your connection and try again.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "PITC form could not be read. Company={Company} ElapsedMs={ElapsedMs}",
                company.Code,
                sw.ElapsedMilliseconds);
            return Fail(company, number, searchBy, ru, ex.Message);
        }
    }

    private async Task<string> RequestBillAsync(
        DiscoCompany company,
        string number,
        string ru,
        string searchBy,
        bool postbackRadio,
        CancellationToken cancellationToken)
    {
        var pageUrl = $"{PitcOutbound.PortalBaseUrl}/{company.Slug}";

        // CookieContainer must be per lookup session; do not share via IHttpClientFactory pooling.
        using var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            UseCookies = true,
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.All
        };
        using var client = new HttpClient(handler)
        {
            Timeout = PitcOutbound.PortalTimeout
        };
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", PitcOutbound.UserAgent);
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml");

        using var get = await client.GetAsync(pageUrl, cancellationToken);
        var html = await get.Content.ReadAsStringAsync(cancellationToken);
        if (!get.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"PITC did not return the {company.ShortName} page ({(int)get.StatusCode}).",
                inner: null,
                statusCode: get.StatusCode);
        }

        if (postbackRadio)
        {
            var radioFields = BuildFields(html, number, ru, "appno", includeButton: false);
            radioFields["__EVENTTARGET"] = "rbSearchByList$1";
            html = await PostFormAsync(client, pageUrl, radioFields, company.ShortName, cancellationToken);
        }

        var fields = BuildFields(html, number, ru, searchBy, includeButton: true);
        return await PostFormAsync(client, pageUrl, fields, company.ShortName, cancellationToken);
    }

    private static async Task<string> PostFormAsync(
        HttpClient client,
        string pageUrl,
        Dictionary<string, string> fields,
        string companyShortName,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(fields);
        using var response = await client.PostAsync(pageUrl, content, cancellationToken);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"PITC did not accept the {companyShortName} search ({(int)response.StatusCode}).",
                inner: null,
                statusCode: response.StatusCode);
        }

        return html;
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
            "${attr}${q}" + PitcOutbound.PortalBaseUrl + "/",
            RegexOptions.IgnoreCase);
        html = Regex.Replace(
            html,
            @"url\(\s*(?<q>['""]?)/(?!/)",
            "url(${q}" + PitcOutbound.PortalBaseUrl + "/",
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
