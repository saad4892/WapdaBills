namespace WapdaBill.Services;

public sealed record RegionOption(string Key, string Label, string Detail);

public sealed class DiscoCompany
{
    public required string Code { get; init; }

    public required string ShortName { get; init; }

    public required string FullName { get; init; }

    public required string[] Regions { get; init; }

    public required string Coverage { get; init; }

    public string? Slug { get; init; }

    public required string Color { get; init; }

    public string? ExternalUrl { get; init; }

    public string? Note { get; init; }

    public bool UsesPitc => !string.IsNullOrWhiteSpace(Slug);
}

public static class DiscoCatalog
{
    public static readonly IReadOnlyList<RegionOption> Regions =
    [
        new("Punjab", "Punjab", "Lahore, Gujranwala, Faisalabad, Multan, Rawalpindi"),
        new("Islamabad", "Islamabad", "Capital territory and Rawalpindi division"),
        new("Khyber Pakhtunkhwa", "Khyber Pakhtunkhwa", "Hazara, Peshawar, merged districts"),
        new("Sindh", "Sindh", "Hyderabad, Sukkur, Karachi"),
        new("Balochistan", "Balochistan", "Quetta and the rest of the province")
    ];

    public static readonly IReadOnlyList<DiscoCompany> All =
    [
        new()
        {
            Code = "lesco",
            ShortName = "LESCO",
            FullName = "Lahore Electric Supply Company",
            Regions = ["Punjab"],
            Coverage = "Lahore, Kasur, Sheikhupura, Nankana Sahib, Okara",
            Slug = "lescobill",
            Color = "#c45c26"
        },
        new()
        {
            Code = "gepco",
            ShortName = "GEPCO",
            FullName = "Gujranwala Electric Power Company",
            Regions = ["Punjab"],
            Coverage = "Gujranwala, Sialkot, Gujrat, Narowal, Hafizabad, Mandi Bahauddin",
            Slug = "gepcobill",
            Color = "#1f7a4d"
        },
        new()
        {
            Code = "fesco",
            ShortName = "FESCO",
            FullName = "Faisalabad Electric Supply Company",
            Regions = ["Punjab"],
            Coverage = "Faisalabad, Sargodha, Jhang, Toba Tek Singh, Chiniot, Mianwali, Khushab, Bhakkar",
            Slug = "fescobill",
            Color = "#b45309"
        },
        new()
        {
            Code = "iesco",
            ShortName = "IESCO",
            FullName = "Islamabad Electric Supply Company",
            Regions = ["Punjab", "Islamabad"],
            Coverage = "Islamabad, Rawalpindi, Attock, Jhelum, Chakwal",
            Slug = "iescobill",
            Color = "#1d4e89"
        },
        new()
        {
            Code = "mepco",
            ShortName = "MEPCO",
            FullName = "Multan Electric Power Company",
            Regions = ["Punjab"],
            Coverage = "Multan, Bahawalpur, Bahawalnagar, Rahim Yar Khan, D.G. Khan, Muzaffargarh, Vehari, Sahiwal",
            Slug = "mepcobill",
            Color = "#9f1239"
        },
        new()
        {
            Code = "hazeco",
            ShortName = "HAZECO",
            FullName = "Hazara Electric Supply Company",
            Regions = ["Khyber Pakhtunkhwa"],
            Coverage = "Abbottabad, Mansehra, Haripur, Battagram, Kohistan",
            Slug = "hazecobill",
            Color = "#0369a1"
        },
        new()
        {
            Code = "pesco",
            ShortName = "PESCO",
            FullName = "Peshawar Electric Supply Company",
            Regions = ["Khyber Pakhtunkhwa"],
            Coverage = "Peshawar, Mardan, Charsadda, Nowshera, Swabi, Kohat, Bannu, D.I. Khan",
            Slug = "pescobill",
            Color = "#0f766e"
        },
        new()
        {
            Code = "tesco",
            ShortName = "TESCO",
            FullName = "Tribal Electric Supply Company",
            Regions = ["Khyber Pakhtunkhwa"],
            Coverage = "Merged tribal districts",
            Slug = "tescobill",
            Color = "#6d28d9"
        },
        new()
        {
            Code = "hesco",
            ShortName = "HESCO",
            FullName = "Hyderabad Electric Supply Company",
            Regions = ["Sindh"],
            Coverage = "Hyderabad, Mirpurkhas, Shaheed Benazirabad, Thatta, Badin",
            Slug = "hescobill",
            Color = "#b45309"
        },
        new()
        {
            Code = "sepco",
            ShortName = "SEPCO",
            FullName = "Sukkur Electric Power Company",
            Regions = ["Sindh"],
            Coverage = "Sukkur, Larkana, Jacobabad, Shikarpur, Khairpur",
            Slug = "sepcobill",
            Color = "#0e7490"
        },
        new()
        {
            Code = "ke",
            ShortName = "K-Electric",
            FullName = "K-Electric",
            Regions = ["Sindh"],
            Coverage = "Karachi and adjoining areas",
            Color = "#15803d",
            ExternalUrl = "https://ke.com.pk/bills-e-payments/",
            Note = "K-Electric is outside WAPDA billing. Its 13-digit account number is not on the PITC portal."
        },
        new()
        {
            Code = "qesco",
            ShortName = "QESCO",
            FullName = "Quetta Electric Supply Company",
            Regions = ["Balochistan"],
            Coverage = "Quetta, Khuzdar, Turbat, Sibi, Loralai, Zhob",
            Slug = "qescobill",
            Color = "#a16207"
        }
    ];

    public static DiscoCompany? Find(string? code) =>
        All.FirstOrDefault(company => string.Equals(company.Code, code, StringComparison.OrdinalIgnoreCase));
}
