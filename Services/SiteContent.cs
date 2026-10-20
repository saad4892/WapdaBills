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
        new("How long does a bill search take?", "The company portal usually answers in 10 to 20 seconds. Leave the page open until the bill appears.")
    ];

    public static readonly IReadOnlyList<FaqItem> HomeFaqs =
    [
        new(
            "How can I check my electricity bill online without a reference number?",
            "Use the Consumer ID / customer ID printed on the bill if the 14-digit reference number is missing. Select your DISCO, enter the customer ID, and tap Check Bill. If neither number is available, contact your DISCO helpline or subdivision office printed on a previous bill."),
        new(
            "What is the difference between Reference Number and Consumer ID?",
            "The reference number is a 14-digit account key used across most WAPDA/PITC DISCOs. The consumer ID (customer ID) is a shorter account number, usually 8–10 digits, shown near the top of the bill. Both can open a duplicate bill on this site; enter whichever you have."),
        new(
            "Can I pay my duplicate bill online via JazzCash or EasyPaisa?",
            "Yes. After you open the duplicate bill, pay the payable amount through JazzCash, EasyPaisa, bank apps, or your DISCO’s published payment channels using the same reference number. This website does not collect payments."),
        new(
            "When does the electricity peak hours rate apply in Pakistan?",
            "Peak (TOU) hours depend on your DISCO, meter type, and season. They are printed on time-of-use bills and company notices. Check the Peak Hours guide on this site, then confirm the schedule on your own bill before estimating charges."),
        new(
            "What should I do if my online bill is not updating?",
            "Wait for the billing cycle to close, re-check the DISCO and number, and try the U/R letter if you use a reference number. If the portal still shows an old bill, verify on the official company channel or ask your subdivision office—upstream systems sometimes lag by a day or two.")
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
