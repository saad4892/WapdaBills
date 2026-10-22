using WapdaBill.Models;

namespace WapdaBill.Models.Cms;

public class RedirectRule
{
    public int Id { get; set; }

    public string FromPath { get; set; } = "";

    public string ToUrl { get; set; } = "";

    public int StatusCode { get; set; } = 301;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class NotFoundLog
{
    public int Id { get; set; }

    public string Path { get; set; } = "";

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public string? UserAgent { get; set; }

    public string? Referrer { get; set; }
}

public class AuditLog
{
    public int Id { get; set; }

    public string Username { get; set; } = "";

    public string Action { get; set; } = "";

    public string EntityType { get; set; } = "";

    public string? EntityId { get; set; }

    public string? Detail { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class GuideFaq
{
    public int Id { get; set; }

    public int BlogPostId { get; set; }

    public BlogPost? BlogPost { get; set; }

    public string Question { get; set; } = "";

    public string AnswerHtml { get; set; } = "";

    public int SortOrder { get; set; }
}
