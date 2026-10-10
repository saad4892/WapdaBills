namespace WapdaBill.Models;

public sealed class BillSummary
{
    public string Name { get; set; } = "";

    public string Reference { get; set; } = "";

    public string ConsumerId { get; set; } = "";

    public string BillMonth { get; set; } = "";

    public string DueDate { get; set; } = "";

    public string PayableWithin { get; set; } = "";

    public string PayableAfter { get; set; } = "";

    public string Units { get; set; } = "";
}
