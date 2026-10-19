namespace WapdaBill.Services;

public sealed record RegionCityLink(
    string City,
    string DiscoShortName,
    string CompanyCode,
    string Href);

public sealed record RegionalHub(
    string PathKey,
    string? RegionKey,
    string H1,
    string MetaTitle,
    string MetaDescription,
    string MetaKeywords,
    string Label,
    IReadOnlyList<string> IntroParagraphs,
    IReadOnlyList<string> GuideBullets);

public static class RegionalHubContent
{
    public static readonly IReadOnlyList<RegionalHub> All =
    [
        new(
            PathKey: "/punjab-electricity-bills",
            RegionKey: "Punjab",
            H1: "Punjab Electricity Bills Online Check & Distribution Companies",
            MetaTitle: "Punjab Electricity Bills Online Check | LESCO MEPCO FESCO GEPCO IESCO",
            MetaDescription: "Check Punjab electricity bills online. Find LESCO, MEPCO, FESCO, GEPCO and IESCO coverage, city mapping, and open a live duplicate bill.",
            MetaKeywords: "Punjab electricity bill online check, LESCO bill, MEPCO bill, FESCO bill, GEPCO bill, IESCO bill",
            Label: "Punjab",
            IntroParagraphs:
            [
                "Punjab’s electricity network is served by multiple distribution companies (DISCOs). Each DISCO covers a fixed set of districts — a Lahore reference number will not open under Multan, and vice versa.",
                "Use this hub to pick LESCO, MEPCO, FESCO, GEPCO, or IESCO, then check your duplicate bill with the 14-digit reference number or consumer ID."
            ],
            GuideBullets:
            [
                "Helpline and complaint numbers are printed on your bill and published by each DISCO — start with our helpline guide, then confirm on the company notice.",
                "For power outages or new connections, contact the subdivision office listed on the bill or the offices directory.",
                "Load-shedding and feeder schedules change by season and locality. Always verify on official DISCO notices or apps — this site does not publish live schedules."
            ]),
        new(
            PathKey: "/sindh-electricity-bills",
            RegionKey: "Sindh",
            H1: "Sindh Electricity Bills Online Check & Distribution Companies",
            MetaTitle: "Sindh Electricity Bills Online Check | HESCO SEPCO K-Electric",
            MetaDescription: "Check Sindh electricity bills online for HESCO, SEPCO and K-Electric. See coverage cities and open a duplicate bill or the K-Electric portal.",
            MetaKeywords: "Sindh electricity bill online check, HESCO bill, SEPCO bill, K-Electric bill Karachi",
            Label: "Sindh",
            IntroParagraphs:
            [
                "Sindh is served by HESCO and SEPCO on the WAPDA/PITC duplicate-bill flow, while Karachi and adjoining areas use K-Electric’s separate billing system.",
                "Choose the company printed on your bill below, then open the checker — or follow the K-Electric path for Karachi account numbers."
            ],
            GuideBullets:
            [
                "Karachi consumers should use K-Electric’s official channels for account and payment questions; HESCO/SEPCO use the reference-number checker on this site.",
                "Complaint and helpline contacts differ by company — see the helpline guide and confirm numbers on your bill.",
                "Outage and load-shedding updates are issued by each DISCO. This portal does not host live feeder schedules."
            ]),
        new(
            PathKey: "/kpk-electricity-bills",
            RegionKey: "Khyber Pakhtunkhwa",
            H1: "KPK Electricity Bills Online Check & Distribution Companies",
            MetaTitle: "KPK Electricity Bills Online Check | PESCO HAZECO TESCO",
            MetaDescription: "Check Khyber Pakhtunkhwa electricity bills online for PESCO, HAZECO and TESCO. Map cities to the right DISCO and open a duplicate bill.",
            MetaKeywords: "KPK electricity bill online check, PESCO bill, HAZECO bill, TESCO bill",
            Label: "Khyber Pakhtunkhwa",
            IntroParagraphs:
            [
                "Khyber Pakhtunkhwa is covered by PESCO, HAZECO (Hazara), and TESCO (merged tribal districts). Selecting the correct company is required for a successful duplicate-bill lookup.",
                "Use the city directory below if you know your city but not the DISCO name printed on older bills."
            ],
            GuideBullets:
            [
                "Use the DISCO helpline guide for starting points, then verify the number printed on your latest bill.",
                "Subdivision offices and coverage districts are listed on the offices page.",
                "Load-shedding schedules are local and change often — check official PESCO/HAZECO/TESCO notices only."
            ]),
        new(
            PathKey: "/balochistan-electricity-bills",
            RegionKey: "Balochistan",
            H1: "Balochistan Electricity Bills Online Check & Distribution Companies",
            MetaTitle: "Balochistan Electricity Bills Online Check | QESCO",
            MetaDescription: "Check Balochistan electricity bills online with QESCO. See coverage cities and open a live duplicate bill with your reference number.",
            MetaKeywords: "Balochistan electricity bill online check, QESCO bill, Quetta electricity bill",
            Label: "Balochistan",
            IntroParagraphs:
            [
                "Most of Balochistan is served by Quetta Electric Supply Company (QESCO). Enter the 14-digit reference number or consumer ID after selecting QESCO in the bill checker.",
                "This hub lists QESCO coverage cities so you can confirm you are on the right distribution company before searching."
            ],
            GuideBullets:
            [
                "QESCO helpline and complaint contacts are on your bill and in our helpline guide — confirm before calling.",
                "Office and district coverage details are on the offices page.",
                "Feeder and load-shedding schedules are published by QESCO; this site does not mirror live outage calendars."
            ]),
        new(
            PathKey: "/kashmir-gb-electricity-bills",
            RegionKey: null,
            H1: "Kashmir & GB Electricity Bills Online Check & Guidance",
            MetaTitle: "Kashmir & GB Electricity Bills | AJKED Guidance",
            MetaDescription: "Guidance for Azad Jammu & Kashmir and Gilgit-Baltistan electricity bill checks. AJKED sits outside the standard mainland DISCO duplicate-bill flow.",
            MetaKeywords: "Kashmir electricity bill, AJKED bill, GB electricity bill, Azad Kashmir bill",
            Label: "Kashmir & GB",
            IntroParagraphs:
            [
                "Azad Jammu & Kashmir electricity accounts are generally handled under AJKED and related local arrangements, which sit outside the standard WAPDA/PITC duplicate-bill flow used by mainland DISCOs.",
                "Use the AJKED guidance page for territory-specific notes, then return to the main checker for Punjab, Sindh, KPK, or Balochistan DISCOs."
            ],
            GuideBullets:
            [
                "For AJK account or payment questions, prefer official AJK electricity channels published locally.",
                "Mainland DISCO helplines do not apply to AJKED accounts — see our helpline guide only for WAPDA-linked companies.",
                "Load-shedding information for Kashmir & GB should be taken from local official notices, not mainland DISCO portals."
            ])
    ];

    public static RegionalHub? FindByPath(string path)
    {
        var normalized = path.TrimEnd('/');
        return All.FirstOrDefault(h =>
            string.Equals(h.PathKey, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<RegionCityLink> GetCities(IEnumerable<DiscoCompany> companies)
    {
        var rows = new List<RegionCityLink>();
        foreach (var company in companies)
        {
            var landing = SiteNav.DiscoLandings.FirstOrDefault(x => x.CompanyCode == company.Code);
            var href = landing?.Href ?? SiteNav.CheckerUrl(company.Code, company.Regions.FirstOrDefault());
            foreach (var part in company.Coverage.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (part.Length == 0) continue;
                rows.Add(new RegionCityLink(part, company.ShortName, company.Code, href));
            }
        }

        return rows
            .GroupBy(r => r.City, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(r => r.City, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
