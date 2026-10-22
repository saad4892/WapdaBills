namespace WapdaBill.Models.Cms;

public class FooterColumnEntity
{
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public int SortOrder { get; set; }

    public bool IsVisible { get; set; } = true;

    public ICollection<FooterLinkEntity> Links { get; set; } = new List<FooterLinkEntity>();
}

public class FooterLinkEntity
{
    public int Id { get; set; }

    public int FooterColumnId { get; set; }

    public FooterColumnEntity? FooterColumn { get; set; }

    public string Title { get; set; } = "";

    public string Href { get; set; } = "";

    public int SortOrder { get; set; }

    public bool IsVisible { get; set; } = true;
}

public class SocialLinkEntity
{
    public int Id { get; set; }

    public string Platform { get; set; } = "";

    public string Label { get; set; } = "";

    public string Href { get; set; } = "";

    public int SortOrder { get; set; }

    public bool IsVisible { get; set; } = true;
}

public class FooterCopy
{
    public int Id { get; set; } = 1;

    public string BrandName { get; set; } = "";

    public string BrandTag { get; set; } = "";

    public string Mission { get; set; } = "";

    public string Disclaimer { get; set; } = "";

    public string CopyrightTemplate { get; set; } = "© {year} WAPDA Bills. All rights reserved.";
}
