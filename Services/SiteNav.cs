namespace WapdaBill.Services;

public sealed record NavLink(
    string Title,
    string Href,
    string? Description = null,
    string? CompanyCode = null,
    string? RegionKey = null);

public sealed record NavGroup(string Id, string Label, IReadOnlyList<NavLink> Links, bool Mega = false);

public sealed record FooterColumn(string Title, IReadOnlyList<NavLink> Links);

public static class SiteNav
{
    public static readonly IReadOnlyList<NavLink> DiscoLandings =
    [
        new("LESCO Bill Online", "/lesco-bill", "Lahore, Sheikhupura, Kasur, Okara", "lesco", "Punjab"),
        new("MEPCO Bill Online", "/mepco-bill", "Multan, Sahiwal, D.G. Khan, Bahawalpur", "mepco", "Punjab"),
        new("IESCO Bill Online", "/iesco-bill", "Islamabad, Rawalpindi, Attock, Jhelum", "iesco", "Islamabad"),
        new("FESCO Bill Online", "/fesco-bill", "Faisalabad, Sargodha, Jhang", "fesco", "Punjab"),
        new("GEPCO Bill Online", "/gepco-bill", "Gujranwala, Sialkot, Gujarat", "gepco", "Punjab"),
        new("PESCO Bill Online", "/pesco-bill", "Peshawar, Swat, Abbottabad", "pesco", "Khyber Pakhtunkhwa"),
        new("HAZECO Bill Online", "/hazeco-bill", "Abbottabad, Mansehra, Haripur", "hazeco", "Khyber Pakhtunkhwa"),
        new("TESCO Bill Online", "/tesco-bill", "Merged tribal districts", "tesco", "Khyber Pakhtunkhwa"),
        new("K-Electric Bill Online", "/kelectric-bill", "Karachi Region", "ke", "Sindh"),
        new("HESCO Bill Online", "/hesco-bill", "Hyderabad, Mirpurkhas", "hesco", "Sindh"),
        new("SEPCO Bill Online", "/sepco-bill", "Sukkur, Larkana", "sepco", "Sindh"),
        new("QESCO Bill Online", "/qesco-bill", "Quetta, Balochistan", "qesco", "Balochistan"),
        new("AJKED Bill Online", "/ajked-bill", "Azad Jammu & Kashmir", null, null)
    ];

    public static readonly IReadOnlyList<NavLink> RegionLandings =
    [
        new("Punjab Electricity Bills", "/punjab-electricity-bills", "LESCO, MEPCO, FESCO, GEPCO, IESCO", null, "Punjab"),
        new("Sindh Electricity Bills", "/sindh-electricity-bills", "HESCO, SEPCO, K-Electric", null, "Sindh"),
        new("KPK Electricity Bills", "/kpk-electricity-bills", "PESCO, HAZECO, TESCO", null, "Khyber Pakhtunkhwa"),
        new("Balochistan Electricity Bills", "/balochistan-electricity-bills", "QESCO", null, "Balochistan"),
        new("Kashmir & GB Electricity Bills", "/kashmir-gb-electricity-bills", "AJKED and related coverage", null, null)
    ];

    public static readonly IReadOnlyList<NavLink> ToolLinks =
    [
        new("Electricity Bill Estimator", "/bill-calculator", "Estimate units and payable amount"),
        new("Peak Hours Timing Chart", "/peak-hours-guide", "Peak and off-peak timing guide"),
        new("Per Unit Electricity Tariff", "/unit-rates-tariff", "Tariff slabs and unit rates")
    ];

    public static readonly IReadOnlyList<NavLink> GuideLinks =
    [
        new("Find 14-Digit Reference Number", "/how-to-check-bill", "Locate reference number and U/R code"),
        new("Online Bill Payment Guide", "/online-bill-payment", "Pay through banks and apps"),
        new("All DISCOs Helpline Numbers", "/complaint-helpline", "Complaint and helpline contacts")
    ];

    public static readonly IReadOnlyList<NavLink> LegalLinks =
    [
        new("About Us", "/about-us"),
        new("Contact Us", "/contact-us"),
        new("Privacy Policy", "/privacy-policy"),
        new("Terms of Service", "/terms-and-conditions"),
        new("Legal Disclaimer", "/disclaimer")
    ];

    public static readonly IReadOnlyList<NavGroup> PrimaryMenus =
    [
        new("discos", "DISCOs", DiscoLandings, Mega: true),
        new("regions", "By Region", RegionLandings),
        new("tools", "Calculators & Utilities", ToolLinks),
        new("guides", "Guides & Support", GuideLinks)
    ];

    public static readonly IReadOnlyList<FooterColumn> FooterColumns =
    [
        new("Popular DISCOs",
        [
            new("LESCO Bill Online", "/lesco-bill", CompanyCode: "lesco"),
            new("MEPCO Bill Online", "/mepco-bill", CompanyCode: "mepco"),
            new("IESCO Bill Online", "/iesco-bill", CompanyCode: "iesco"),
            new("FESCO Bill Online", "/fesco-bill", CompanyCode: "fesco"),
            new("K-Electric Bill Online", "/kelectric-bill", CompanyCode: "ke"),
            new("GEPCO Bill Online", "/gepco-bill", CompanyCode: "gepco"),
            new("PESCO Bill Online", "/pesco-bill", CompanyCode: "pesco")
        ]),
        new("Tools & Guides",
        [
            new("Bill Estimator Calculator", "/bill-calculator"),
            new("Peak Hours Chart", "/peak-hours-guide"),
            new("Per Unit Electricity Tariff", "/unit-rates-tariff"),
            new("Online Bill Payment Guide", "/online-bill-payment"),
            new("Find Reference Number Guide", "/how-to-check-bill"),
            new("All DISCOs Helpline Numbers", "/complaint-helpline")
        ]),
        new("Regional Utility Portals",
        [
            new("Punjab Electricity Bills", "/punjab-electricity-bills"),
            new("Sindh Electricity Bills", "/sindh-electricity-bills"),
            new("KPK Electricity Bills", "/kpk-electricity-bills"),
            new("Balochistan Electricity Bills", "/balochistan-electricity-bills"),
            new("Kashmir & GB Electricity Bills", "/kashmir-gb-electricity-bills")
        ]),
        new("Legal & Policy", LegalLinks)
    ];

    public static IEnumerable<string> AllPublicPaths()
    {
        yield return "/";
        yield return "/find-number";
        yield return "/resources";
        yield return "/guides";
        yield return "/faqs";
        yield return "/offices";

        foreach (var link in DiscoLandings.Concat(RegionLandings).Concat(ToolLinks).Concat(GuideLinks).Concat(LegalLinks))
        {
            yield return link.Href;
        }
    }

    public static NavLink? FindDiscoByPath(string path) =>
        DiscoLandings.FirstOrDefault(link =>
            string.Equals(Normalize(link.Href), Normalize(path), StringComparison.OrdinalIgnoreCase));

    public static NavLink? FindRegionByPath(string path) =>
        RegionLandings.FirstOrDefault(link =>
            string.Equals(Normalize(link.Href), Normalize(path), StringComparison.OrdinalIgnoreCase));

    public static string CheckerUrl(string? companyCode, string? regionKey = null)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(companyCode))
        {
            query.Add("company=" + Uri.EscapeDataString(companyCode));
        }

        if (!string.IsNullOrWhiteSpace(regionKey))
        {
            query.Add("region=" + Uri.EscapeDataString(regionKey));
        }

        return query.Count == 0 ? "/" : "/?" + string.Join("&", query);
    }

    private static string Normalize(string path) => path.TrimEnd('/');
}
