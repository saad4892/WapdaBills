namespace WapdaBill.Models;

public class JobListing
{
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public string Organization { get; set; } = "";

    public string Location { get; set; } = "";

    public string EmploymentType { get; set; } = "Career notice";

    public string Summary { get; set; } = "";

    public string Body { get; set; } = "";

    public string? ApplyUrl { get; set; }

    public string MetaDescription { get; set; } = "";

    public string MetaKeywords { get; set; } = "";

    public string? ImagePath { get; set; }

    public bool IsPublished { get; set; } = true;

    public DateTime? Deadline { get; set; }

    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
