namespace WapdaBill.Models.Cms;

public class HomepageSection
{
    public int Id { get; set; }

    public string Key { get; set; } = "";

    public string Title { get; set; } = "";

    public string? Lead { get; set; }

    public int SortOrder { get; set; }

    public bool IsVisible { get; set; } = true;

    public ICollection<HomepageItem> Items { get; set; } = new List<HomepageItem>();
}

public class HomepageItem
{
    public int Id { get; set; }

    public int HomepageSectionId { get; set; }

    public HomepageSection? HomepageSection { get; set; }

    public string Title { get; set; } = "";

    public string Body { get; set; } = "";

    public string? Href { get; set; }

    public string? ExtraJson { get; set; }

    public int SortOrder { get; set; }

    public bool IsVisible { get; set; } = true;
}

public class FaqEntity
{
    public int Id { get; set; }

    /// <summary>home or global.</summary>
    public string Scope { get; set; } = "global";

    public string Question { get; set; } = "";

    public string AnswerHtml { get; set; } = "";

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; } = true;
}
