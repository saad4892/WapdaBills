namespace WapdaBill.Models.Cms;

public class MediaAsset
{
    public int Id { get; set; }

    public string FileName { get; set; } = "";

    public string StoragePath { get; set; } = "";

    public string ContentType { get; set; } = "";

    public long ByteSize { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public string AltText { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
