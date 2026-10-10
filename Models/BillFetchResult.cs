using WapdaBill.Services;

namespace WapdaBill.Models;

public sealed class BillFetchResult
{
    public bool Success { get; init; }

    public string? Error { get; init; }

    public string DisplayHtml { get; init; } = "";

    public BillSummary Summary { get; init; } = new();

    public string RuUsed { get; init; } = "";

    public string Number { get; init; } = "";

    public string SearchBy { get; init; } = "refno";

    public DiscoCompany? Company { get; init; }

    public string CacheKey => $"{Company?.Code}|{SearchBy}|{Number}|{RuUsed}";
}
