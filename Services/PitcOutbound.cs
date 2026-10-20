namespace WapdaBill.Services;

/// <summary>Shared PITC outbound HTTP client names and defaults.</summary>
public static class PitcOutbound
{
    public const string PortalClientName = "PitcPortal";
    public const string SnapsClientName = "PitcSnaps";

    public const string PortalBaseUrl = "https://bill.pitc.com.pk";
    public const string SnapsUrl = "https://usersnap.pitc.com.pk/api/SnapsForDuplicateBill/ToDuplicate";

    public static readonly TimeSpan PortalTimeout = TimeSpan.FromSeconds(40);
    public static readonly TimeSpan SnapsTimeout = TimeSpan.FromSeconds(30);

    public const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";
}
