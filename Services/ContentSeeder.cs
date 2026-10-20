using WapdaBill.Data;
using WapdaBill.Models;

namespace WapdaBill.Services;

public static class ContentSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!db.SiteSettings.Any())
        {
            db.SiteSettings.Add(new SiteSetting
            {
                Id = 1,
                SiteTitle = "Pakistan Electricity Bills | LESCO, MEPCO, FESCO and every DISCO",
                MetaDescription = "Check a duplicate electricity bill for LESCO, MEPCO, FESCO, GEPCO, IESCO, PESCO, HAZECO, HESCO, SEPCO, QESCO and TESCO. One reference-number lookup for every WAPDA distribution company in Pakistan.",
                MetaKeywords = "Pakistan electricity bill, LESCO bill online, MEPCO duplicate bill, FESCO bill check, IESCO bill, GEPCO bill, PESCO bill, HESCO bill, SEPCO bill, QESCO bill, TESCO bill, HAZECO bill, WAPDA reference number, K-Electric"
            });
        }

        if (!db.BlogPosts.Any())
        {
            var now = DateTime.UtcNow;
            db.BlogPosts.AddRange(
                Guide(now, "How to find an electricity reference number", "how-to-find-an-electricity-reference-number",
                    "The 14-digit reference number is the key to a duplicate WAPDA bill.",
                    "reference number, duplicate bill, 14 digit, electricity bill Pakistan",
                    """
                    Every distribution company on this site looks up the latest bill with the same kind of reference number. It is printed at the top left of a paper bill and it stays with the meter, not with the person who pays it.

                    ## Where to look

                    Count 14 digits. A letter, U or R, is printed beside them. Type the digits here and choose the matching letter. If the letter is hard to read, leave U selected. The lookup tries R on its own when U does not match.

                    ## Customer ID

                    Newer bills also show a shorter customer ID above the reference number. Switch the search to Customer ID when that is the only number you have.

                    ## What this does not change

                    A duplicate bill is a copy of the bill your company already issued. It does not change units, due date, or the amount payable.
                    """),
                Guide(now.AddMinutes(-1), "What U and R mean on a bill", "what-u-and-r-mean-on-a-bill",
                    "The small letter after the reference number decides which record the portal opens.",
                    "U R code, bill reference, rural urban electricity bill",
                    """
                    Pakistani electricity bills end the reference line with U or R. The online form asks for that letter separately from the 14 digits.

                    ## How to use it

                    Match the letter printed on the bill. U is the usual choice. R is the other record marker. Entering the wrong letter is the most common reason a valid number appears to have no bill.

                    ## If the print is unclear

                    Try U first. This site then asks the same company for R before it tells you the bill was not found. You do not need to run the search twice.
                    """),
                Guide(now.AddMinutes(-2), "Which DISCO covers your city", "which-disco-covers-your-city",
                    "Pick the distribution company for your city before you search a reference number.",
                    "LESCO MEPCO FESCO IESCO GEPCO PESCO HESCO SEPCO QESCO TESCO HAZECO",
                    """
                    A reference number belongs to one distribution company. Searching it under the wrong company returns no bill.

                    ## Punjab

                    - LESCO covers Lahore, Kasur, Sheikhupura, Nankana Sahib and Okara.
                    - GEPCO covers Gujranwala, Sialkot, Gujrat, Narowal, Hafizabad and Mandi Bahauddin.
                    - FESCO covers Faisalabad, Sargodha, Jhang and the nearby districts.
                    - MEPCO covers Multan, Bahawalpur, Dera Ghazi Khan, Rahim Yar Khan and south Punjab.
                    - IESCO covers Islamabad, Rawalpindi, Attock, Jhelum and Chakwal.

                    ## Other provinces

                    - HAZECO, PESCO and TESCO cover Khyber Pakhtunkhwa, including Hazara and the merged districts.
                    - HESCO and SEPCO cover Sindh outside Karachi. K-Electric uses its own 13-digit account number.
                    - QESCO covers Quetta and most of Balochistan.
                    """),
                Guide(now.AddMinutes(-3), "How to spot a fake WAPDA job fee", "how-to-spot-a-fake-wapda-job-fee",
                    "A real distribution-company vacancy does not start with a private fee demand.",
                    "WAPDA jobs, fake recruitment, DISCO careers",
                    """
                    Recruitment for a distribution company is announced by that company. A message that asks for a fee, a jazzcash transfer, or a WhatsApp form before any official notice is not a job offer.

                    ## Check these points

                    - The notice names the company, the posts, and a closing date.
                    - The application address is on that company's own website.
                    - No one asks you to pay this website, or a middleman, to "confirm" a WAPDA seat.

                    ## What this guide is

                    This guide explains where to follow official recruitment notices. It is not a vacancy ad, and this site does not take applications or payments.
                    """));
        }

        if (!db.JobListings.Any())
        {
            var now = DateTime.UtcNow;
            db.JobListings.AddRange(
                Notice(now, "Where LESCO publishes recruitment", "LESCO", "Lahore and central Punjab",
                    "LESCO announces its own posts. Read the company notice before you apply anywhere else.",
                    "LESCO jobs, Lahore electric supply careers",
                    """
                    Lahore Electric Supply Company publishes recruitment on its own channels. A Facebook graphic or a paid agent is not a substitute for that notice.

                    ## Before you apply

                    - Confirm the post title and the last date on the company notice.
                    - Use only the application method written there.
                    - Ignore anyone who asks for a fee to forward your papers.

                    This page is a guide. It is not an open vacancy and it does not collect CVs.
                    """),
                Notice(now.AddMinutes(-1), "Where MEPCO publishes recruitment", "MEPCO", "South Punjab",
                    "Multan Electric Power Company posts its own jobs. Match the notice to the company before you share documents.",
                    "MEPCO jobs, Multan electricity careers",
                    """
                    MEPCO recruitment belongs on MEPCO's own notice, with the posts and eligibility written by the company.

                    ## A safe check

                    - The notice should name MEPCO, not a generic "WAPDA jobs" page.
                    - Closing dates and test cities come from that notice.
                    - Do not send copies of your CNIC to an unofficial number.

                    This listing does not accept applications.
                    """),
                Notice(now.AddMinutes(-2), "Where PESCO and HAZECO publish recruitment", "PESCO and HAZECO", "Khyber Pakhtunkhwa",
                    "Peshawar and Hazara are separate companies. A job for one is not a job for the other.",
                    "PESCO jobs, HAZECO jobs, KPK electricity careers",
                    """
                    PESCO and HAZECO each announce their own posts. Hazara districts are no longer a reason to assume a Peshawar notice applies.

                    ## What to compare

                    - Company name on the notice.
                    - Duty station and required domicile, if the notice asks for one.
                    - The official last date, not a date added by a repost.

                    No application is taken on this website.
                    """),
                Notice(now.AddMinutes(-3), "Where PITC publishes technology roles", "PITC", "Lahore",
                    "Power Information Technology Company hires for its own technology work. Those notices are separate from DISCO line-staff jobs.",
                    "PITC jobs, WAPDA IT jobs, billing technology careers",
                    """
                    PITC is the technology company behind distribution billing. Its vacancies, when they exist, are announced by PITC. They are not the same as a lineman or meter-reader post at LESCO or MEPCO.

                    ## Keep the sources apart

                    - A DISCO notice hires for that company's offices and field staff.
                    - A PITC notice hires for PITC.
                    - A page that mixes both, and then asks for money, is not an official advertisement.

                    Use this entry as a reminder of where to look. It is not a live opening.
                    """));
        }

        await db.SaveChangesAsync();
    }

    private static BlogPost Guide(DateTime published, string title, string slug, string excerpt, string keywords, string body) =>
        new()
        {
            Title = title,
            Slug = slug,
            Excerpt = excerpt,
            Body = body.Trim(),
            MetaDescription = excerpt,
            MetaKeywords = keywords,
            IsPublished = true,
            PublishedAt = published,
            UpdatedAt = published
        };

    private static JobListing Notice(DateTime published, string title, string organization, string location, string summary, string keywords, string body) =>
        new()
        {
            Title = title,
            Organization = organization,
            Location = location,
            EmploymentType = "Career notice",
            Summary = summary,
            Body = body.Trim(),
            MetaDescription = summary,
            MetaKeywords = keywords,
            IsPublished = true,
            PublishedAt = published,
            UpdatedAt = published
        };
}
