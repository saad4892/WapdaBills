namespace WapdaBill.Services;

public sealed record FaqItem(string Question, string Answer);

public sealed record ResourceLink(string Title, string Detail, string Href);

public static class SiteContent
{
    public static readonly IReadOnlyList<ResourceLink> Resources =
    [
        new("Bill check", "Open the latest duplicate bill with a 14-digit reference number.", "/"),
        new("Find number", "See where the reference number, customer ID, and U or R letter sit on a bill.", "/find-number"),
        new("Company coverage", "Match Lahore, Multan, Peshawar, Karachi, and Quetta to the right company.", "/guides/which-disco-covers-your-city"),
        new("U and R code", "Why the small letter after the reference number changes the result.", "/guides/what-u-and-r-mean-on-a-bill"),
        new("FAQs", "Short answers on duplicate bills, customer IDs, and K-Electric.", "/faqs"),
        new("Office locations", "Headquarters city and service area for every distribution company.", "/offices"),
        new("Career notices", "Where companies publish recruitment. This site does not take fees or CVs.", "/careers"),
        new("Guides", "Longer notes on reading a bill and avoiding fake job offers.", "/guides")
    ];

    public static readonly IReadOnlyList<FaqItem> Faqs =
    [
        new("Where is the reference number?", "It is the 14-digit number at the top left of a paper bill. A letter, U or R, is printed beside it. The customer ID, when shown, sits just above the reference number."),
        new("Which company should I select?", "Select the distribution company printed on the bill. A number from one company will not open on another. Use Find Number and the coverage guide if you only know the city."),
        new("What if U does not open the bill?", "Try R, or leave U selected. This site asks for R automatically when U returns no bill."),
        new("Does this include K-Electric?", "No. Karachi’s K-Electric uses a 13-digit account number on ke.com.pk. The other companies on this site use the WAPDA reference number."),
        new("Can I pay with the duplicate bill?", "The duplicate is a copy of the bill already issued for that reference number. Banks and wallet apps usually accept the reference number itself."),
        new("How do I find the nearest office?", "The subdivision office is printed on the bill. The Offices page lists each company’s headquarters city and the districts it serves."),
        new("Are the career pages job ads?", "No. They explain where a company publishes its own notices. This site does not collect applications or payments."),
        new("How long does a bill search take?", "The company portal usually answers in 10 to 20 seconds. Leave the page open until the bill appears.")
    ];

    public static string Headquarters(string code) => code switch
    {
        "lesco" => "Lahore",
        "gepco" => "Gujranwala",
        "fesco" => "Faisalabad",
        "iesco" => "Islamabad",
        "mepco" => "Multan",
        "hazeco" => "Abbottabad",
        "pesco" => "Peshawar",
        "tesco" => "Peshawar",
        "hesco" => "Hyderabad",
        "sepco" => "Sukkur",
        "qesco" => "Quetta",
        "ke" => "Karachi",
        _ => "Pakistan"
    };
}
