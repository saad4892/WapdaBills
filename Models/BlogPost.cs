namespace WapdaBill.Models;

public class BlogPost
{
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public string Slug { get; set; } = "";

    public string Excerpt { get; set; } = "";

    public string Body { get; set; } = "";

    public string MetaDescription { get; set; } = "";

    public string MetaKeywords { get; set; } = "";

    public string? ImagePath { get; set; }

    public bool IsPublished { get; set; } = true;

    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
