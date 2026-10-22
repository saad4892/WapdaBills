namespace WapdaBill.Models.Cms;

public class ContentPage
{
    public int Id { get; set; }

    public string Slug { get; set; } = "";

    /// <summary>Legal, Tool, Info, GuideStatic, Generic.</summary>
    public string Template { get; set; } = "Generic";

    public string Title { get; set; } = "";

    public string H1 { get; set; } = "";

    public string BodyHtml { get; set; } = "";

    public string? IntroHtml { get; set; }

    public string MetaTitle { get; set; } = "";

    public string MetaDescription { get; set; } = "";

    public string MetaKeywords { get; set; } = "";

    public string? CanonicalPath { get; set; }

    public int? OgImageMediaId { get; set; }

    public MediaAsset? OgImageMedia { get; set; }

    public string Robots { get; set; } = "index, follow";

    public string Language { get; set; } = "en";

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
