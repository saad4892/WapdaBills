namespace WapdaBill.Models.Cms;

public class NavMenu
{
    public int Id { get; set; }

    public string Key { get; set; } = "";

    public string Label { get; set; } = "";

    public int SortOrder { get; set; }

    public bool IsVisible { get; set; } = true;

    public ICollection<NavItem> Items { get; set; } = new List<NavItem>();
}

public class NavItem
{
    public int Id { get; set; }

    public int NavMenuId { get; set; }

    public NavMenu? NavMenu { get; set; }

    public int? ParentId { get; set; }

    public NavItem? Parent { get; set; }

    public ICollection<NavItem> Children { get; set; } = new List<NavItem>();

    public string Title { get; set; } = "";

    public string Href { get; set; } = "";

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsVisible { get; set; } = true;

    public bool IsMega { get; set; }
}
