using WapdaBill.Models.Cms;

namespace WapdaBill.Models;

public class SiteSetting
{
    public int Id { get; set; }

    public string SiteName { get; set; } = "WAPDA Bills";

    public string SiteTitle { get; set; } = "";

    public string MetaDescription { get; set; } = "";

    public string MetaKeywords { get; set; } = "";

    public int? LogoMediaId { get; set; }

    public MediaAsset? LogoMedia { get; set; }

    public int? FaviconMediaId { get; set; }

    public MediaAsset? FaviconMedia { get; set; }

    public int? DefaultOgImageMediaId { get; set; }

    public MediaAsset? DefaultOgImageMedia { get; set; }

    public string? GoogleSearchConsoleVerification { get; set; }

    public string? Ga4MeasurementId { get; set; }

    public string? GtmContainerId { get; set; }
}
