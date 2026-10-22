namespace WapdaBill.Models.Cms;

public class DiscoEntity
{
    public int Id { get; set; }

    /// <summary>Stable operational code (lesco, mepco, …).</summary>
    public string Code { get; set; } = "";

    public string ShortName { get; set; } = "";

    public string FullName { get; set; } = "";

    public string PublicSlug { get; set; } = "";

    public string Coverage { get; set; } = "";

    public string RegionsJson { get; set; } = "[]";

    public string? PitcSlug { get; set; }

    public bool UsesPitc { get; set; }

    public string Color { get; set; } = "#0B2A5B";

    public string? ExternalUrl { get; set; }

    public string? Note { get; set; }

    public string IntroHtml { get; set; } = "";

    public string BodyHtml { get; set; } = "";

    public int? LogoMediaId { get; set; }

    public MediaAsset? LogoMedia { get; set; }

    public string MetaTitle { get; set; } = "";

    public string MetaDescription { get; set; } = "";

    public string MetaKeywords { get; set; } = "";

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class RegionHubEntity
{
    public int Id { get; set; }

    public string Path { get; set; } = "";

    public string RegionKey { get; set; } = "";

    public string Label { get; set; } = "";

    public string H1 { get; set; } = "";

    public string IntroHtml { get; set; } = "";

    public string BodyHtml { get; set; } = "";

    public string MetaTitle { get; set; } = "";

    public string MetaDescription { get; set; } = "";

    public string MetaKeywords { get; set; } = "";

    public bool IsPublished { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
