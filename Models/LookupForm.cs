namespace WapdaBill.Models;

public sealed class LookupForm
{
    public string Region { get; set; } = "Punjab";

    public string? Company { get; set; }

    public string SearchBy { get; set; } = "refno";

    public string? Number { get; set; }

    public string Ru { get; set; } = "";

    public string? Error { get; set; }
}
