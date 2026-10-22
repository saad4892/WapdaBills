using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WapdaBill.Data;
using WapdaBill.Models.Cms;

namespace WapdaBill.Services;

public static class CmsSeeder
{
    public static async Task SeedCmsAsync(AppDbContext db)
    {
        await SeedFooterAsync(db);
        await SeedSocialAsync(db);
        await SeedNavAsync(db);
        await SeedDiscosAsync(db);
        await SeedRegionHubsAsync(db);
        await SeedFaqsAsync(db);
        await SeedHomepageAsync(db);
        await SeedContentPagesAsync(db);
        await db.SaveChangesAsync();
    }

    private static async Task SeedFooterAsync(AppDbContext db)
    {
        if (!await db.FooterCopies.AnyAsync())
        {
            db.FooterCopies.Add(new FooterCopy
            {
                Id = 1,
                BrandName = "Electricity Bill Checker Pakistan",
                BrandTag = "Free public utility helper for DISCO duplicate bills",
                Mission = "A free public utility helper that helps consumers check publicly available duplicate electricity bills online across Pakistan’s distribution companies.",
                Disclaimer = "Disclaimer: This website is a privately owned informational platform created to assist consumers in checking publicly available duplicate electricity bills online. This website is NOT affiliated with, endorsed by, or connected to WAPDA, NEPRA, or any Government Distribution Company (LESCO, MEPCO, IESCO, K-Electric, PESCO, FESCO, GEPCO, HESCO, SEPCO, QESCO). All brand logos and names belong to their respective official owners.",
                CopyrightTemplate = "© {year} WAPDA Bills. All rights reserved."
            });
        }

        if (await db.FooterColumns.AnyAsync())
        {
            return;
        }

        var order = 0;
        foreach (var col in SiteNav.FooterColumns)
        {
            var entity = new FooterColumnEntity
            {
                Title = col.Title,
                SortOrder = order++,
                IsVisible = true
            };
            var linkOrder = 0;
            foreach (var link in col.Links)
            {
                entity.Links.Add(new FooterLinkEntity
                {
                    Title = link.Title,
                    Href = link.Href,
                    SortOrder = linkOrder++,
                    IsVisible = true
                });
            }

            db.FooterColumns.Add(entity);
        }
    }

    private static async Task SeedSocialAsync(AppDbContext db)
    {
        if (await db.SocialLinks.AnyAsync())
        {
            return;
        }

        var order = 0;
        foreach (var s in SiteNav.SocialLinks)
        {
            db.SocialLinks.Add(new SocialLinkEntity
            {
                Platform = s.Id,
                Label = s.Label,
                Href = s.Href,
                SortOrder = order++,
                IsVisible = true
            });
        }
    }

    private static async Task SeedNavAsync(AppDbContext db)
    {
        if (await db.NavMenus.AnyAsync())
        {
            return;
        }

        var order = 0;
        foreach (var menu in SiteNav.PrimaryMenus)
        {
            var entity = new NavMenu
            {
                Key = menu.Id,
                Label = menu.Label,
                SortOrder = order++,
                IsVisible = true
            };
            var linkOrder = 0;
            foreach (var link in menu.Links)
            {
                entity.Items.Add(new NavItem
                {
                    Title = link.Title,
                    Href = link.Href,
                    Description = link.Description,
                    SortOrder = linkOrder++,
                    IsVisible = true,
                    IsMega = menu.Mega
                });
            }

            db.NavMenus.Add(entity);
        }
    }

    private static async Task SeedDiscosAsync(AppDbContext db)
    {
        if (await db.Discos.AnyAsync())
        {
            return;
        }

        var order = 0;
        foreach (var link in SiteNav.DiscoLandings)
        {
            var company = string.IsNullOrWhiteSpace(link.CompanyCode)
                ? null
                : DiscoCatalog.Find(link.CompanyCode);
            db.Discos.Add(new DiscoEntity
            {
                Code = company?.Code ?? link.Href.Trim('/').Replace("-bill", ""),
                ShortName = company?.ShortName ?? link.Title.Split(' ')[0],
                FullName = company?.FullName ?? link.Title,
                PublicSlug = link.Href.Trim('/'),
                Coverage = company?.Coverage ?? link.Description ?? "",
                RegionsJson = JsonSerializer.Serialize(company?.Regions ?? Array.Empty<string>()),
                PitcSlug = company?.Slug,
                UsesPitc = company?.UsesPitc ?? false,
                Color = company?.Color ?? "#0B2A5B",
                ExternalUrl = company?.ExternalUrl,
                Note = company?.Note,
                IntroHtml = $"<p>{System.Net.WebUtility.HtmlEncode(company?.FullName ?? link.Title)}. {System.Net.WebUtility.HtmlEncode(link.Description ?? "")}</p>",
                BodyHtml = "",
                MetaTitle = $"{link.Title} Online | WAPDA Bills",
                MetaDescription = company is null
                    ? $"Check {link.Title} online. {link.Description}."
                    : $"Check {company.ShortName} bill online for {company.Coverage}.",
                MetaKeywords = $"{company?.ShortName ?? link.Title} bill online",
                SortOrder = order++,
                IsPublished = true,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }

    private static async Task SeedRegionHubsAsync(AppDbContext db)
    {
        if (await db.RegionHubs.AnyAsync())
        {
            return;
        }

        foreach (var hub in RegionalHubContent.All)
        {
            var path = hub.PathKey.Trim('/');
            db.RegionHubs.Add(new RegionHubEntity
            {
                Path = path,
                RegionKey = hub.RegionKey ?? "",
                Label = hub.Label,
                H1 = hub.H1,
                IntroHtml = string.Join("", hub.IntroParagraphs.Select(p => $"<p>{System.Net.WebUtility.HtmlEncode(p)}</p>")),
                BodyHtml = "",
                MetaTitle = hub.MetaTitle,
                MetaDescription = hub.MetaDescription,
                MetaKeywords = hub.MetaKeywords,
                IsPublished = true,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }

    private static async Task SeedFaqsAsync(AppDbContext db)
    {
        if (await db.Faqs.AnyAsync())
        {
            return;
        }

        var order = 0;
        foreach (var faq in SiteContent.Faqs)
        {
            db.Faqs.Add(new FaqEntity
            {
                Scope = "global",
                Question = faq.Question,
                AnswerHtml = $"<p>{System.Net.WebUtility.HtmlEncode(faq.Answer)}</p>",
                SortOrder = order++,
                IsPublished = true
            });
        }

        order = 0;
        foreach (var faq in SiteContent.HomeFaqs)
        {
            db.Faqs.Add(new FaqEntity
            {
                Scope = "home",
                Question = faq.Question,
                AnswerHtml = $"<p>{System.Net.WebUtility.HtmlEncode(faq.Answer)}</p>",
                SortOrder = order++,
                IsPublished = true
            });
        }
    }

    private static async Task SeedHomepageAsync(AppDbContext db)
    {
        if (await db.HomepageSections.AnyAsync())
        {
            return;
        }

        void AddSection(string key, string title, string? lead, params (string Title, string Body, string? Href)[] items)
        {
            var section = new HomepageSection
            {
                Key = key,
                Title = title,
                Lead = lead,
                SortOrder = db.HomepageSections.Local.Count + db.HomepageSections.Count(),
                IsVisible = true
            };
            var i = 0;
            foreach (var item in items)
            {
                section.Items.Add(new HomepageItem
                {
                    Title = item.Title,
                    Body = item.Body,
                    Href = item.Href,
                    SortOrder = i++,
                    IsVisible = true
                });
            }

            db.HomepageSections.Add(section);
        }

        AddSection("hero", "Check Electricity Bill Online in Pakistan",
            "Fast, free, live duplicate bills for LESCO, MEPCO, IESCO, K-Electric, FESCO, PESCO, GEPCO, HESCO, SEPCO, and QESCO — plus HAZECO and TESCO.",
            ("eyebrow", "Free duplicate bill checker", null),
            ("bullet1", "Free, with no login", null),
            ("bullet2", "All 12 distribution companies", null),
            ("bullet3", "Works on mobile", null));

        AddSection("why-choose", "Why choose WAPDA Bills", null,
            ("All major DISCOs", "One checker for LESCO, MEPCO, IESCO, and every other WAPDA company.", null),
            ("Live duplicate bills", "Opens the latest available official duplicate from the upstream record.", null),
            ("Free and private", "No payment collection; use only your reference number or consumer ID.", null));

        AddSection("how-check", "How to View & Download Your Online Electricity Bill", null,
            ("Locate your reference number", "Find the 14-digit reference number on your previous utility bill copy (top left).", null),
            ("Select your DISCO", "Choose your electricity distribution company from the menu above.", null),
            ("Enter and check", "Type your reference number or consumer ID into the input field and click Check Bill.", null),
            ("View or print", "View, print, or save your official duplicate bill from the result page.", null));

        AddSection("tools", "Electricity Tools & Calculators", null,
            ("Bill Estimator / Calculator", "Estimate your monthly bill from consumed units, then open the live duplicate for the exact payable amount.", "/bill-calculator"),
            ("Peak Hours Timings Chart", "Peak vs off-peak timing guidance for time-of-use meters across Pakistan DISCOs.", "/peak-hours-guide"),
            ("Latest NEPRA Unit Rates (Tariff)", "Overview of electricity slab pricing. Always verify payable amounts on your official bill.", "/unit-rates-tariff"));

        AddSection("charges", "Understanding Your Electricity Bill Taxes & Surcharges",
            "Pakistani utility bills often include more than energy charges.",
            ("Fuel Price Adjustment (FPA)", "A monthly adjustment reflecting changes in generation fuel costs.", null),
            ("Neelum Jhelum Surcharge (NJ)", "A surcharge linked to the Neelum–Jhelum hydropower project.", null),
            ("Electricity Duty", "A provincial duty applied on electricity consumption.", null),
            ("TV Fee", "A fixed monthly television fee collected with electricity bills where applicable.", null),
            ("GST / Sales Tax", "Sales tax applied on taxable components of the bill.", null),
            ("Quarter Tariff Adjustment (QTR)", "A periodic adjustment reconciling earlier tariff periods.", null));
    }

    private static async Task SeedContentPagesAsync(AppDbContext db)
    {
        if (await db.ContentPages.AnyAsync())
        {
            return;
        }

        void AddLegal(string slug, string title, string h1, IReadOnlyList<LegalSection> sections, string metaDesc)
        {
            var body = string.Join("", sections.Select(s =>
            {
                var paras = string.Join("", s.Paragraphs.Select(p => $"<p>{System.Net.WebUtility.HtmlEncode(p)}</p>"));
                var bullets = s.Bullets is { Count: > 0 }
                    ? "<ul>" + string.Join("", s.Bullets.Select(b => $"<li>{System.Net.WebUtility.HtmlEncode(b)}</li>")) + "</ul>"
                    : "";
                return $"<h2>{System.Net.WebUtility.HtmlEncode(s.Heading)}</h2>{paras}{bullets}";
            }));
            db.ContentPages.Add(new ContentPage
            {
                Slug = slug,
                Template = "Legal",
                Title = title,
                H1 = h1,
                BodyHtml = body,
                MetaTitle = title + " | WAPDA Bills",
                MetaDescription = metaDesc,
                CanonicalPath = "/" + slug,
                IsPublished = true,
                UpdatedAt = DateTime.UtcNow
            });
        }

        AddLegal("about-us", "About Us", "About WAPDA Bills", LegalContent.About,
            "Learn about WAPDA Bills, an independent helper for checking Pakistan electricity duplicate bills.");
        AddLegal("privacy-policy", "Privacy Policy", "Privacy Policy", LegalContent.Privacy,
            "How WAPDA Bills handles reference numbers, contact messages, and site analytics.");
        AddLegal("terms-and-conditions", "Terms of Service", "Terms of Service", LegalContent.Terms,
            "Terms for using the WAPDA Bills electricity bill checker website.");
        AddLegal("disclaimer", "Legal Disclaimer", "Legal Disclaimer", LegalContent.Disclaimer,
            "Independent third-party disclaimer for WAPDA Bills and Pakistan DISCO bill lookups.");

        db.ContentPages.Add(new ContentPage
        {
            Slug = "bill-calculator",
            Template = "Tool",
            Title = "Electricity Bill Estimator",
            H1 = "Electricity Bill Estimator",
            IntroHtml = "<p>Enter your units on the official company portal after estimating. This page helps you pick the right DISCO and open the live bill checker.</p>",
            BodyHtml = "",
            MetaTitle = "Electricity Bill Calculator | WAPDA Bills",
            MetaDescription = "Estimate electricity units and payable amount, then open a live duplicate bill for your DISCO.",
            CanonicalPath = "/bill-calculator",
            IsPublished = true,
            UpdatedAt = DateTime.UtcNow
        });
        db.ContentPages.Add(new ContentPage
        {
            Slug = "peak-hours-guide",
            Template = "Tool",
            Title = "Peak Hours Timing",
            H1 = "Peak Hours Timing",
            IntroHtml = "<p>Peak hours vary by company and season. Confirm the latest TOU schedule on your bill or company notice, then check your bill here.</p>",
            BodyHtml = "",
            MetaTitle = "Peak Hours Guide Pakistan | WAPDA Bills",
            MetaDescription = "Peak and off-peak electricity timing guidance for Pakistan DISCOs.",
            CanonicalPath = "/peak-hours-guide",
            IsPublished = true,
            UpdatedAt = DateTime.UtcNow
        });
        db.ContentPages.Add(new ContentPage
        {
            Slug = "unit-rates-tariff",
            Template = "Tool",
            Title = "Unit Rates & Tariff",
            H1 = "Unit Rates & Tariff",
            IntroHtml = "<p>Tariff slabs change with NEPRA notifications. Use this hub to reach your DISCO and open the latest payable amount from the official record.</p>",
            BodyHtml = "",
            MetaTitle = "Unit Rates Tariff Pakistan | WAPDA Bills",
            MetaDescription = "NEPRA slab overview and links to check your live electricity bill.",
            CanonicalPath = "/unit-rates-tariff",
            IsPublished = true,
            UpdatedAt = DateTime.UtcNow
        });
    }
}
