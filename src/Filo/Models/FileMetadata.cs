namespace ManuHub.Filo;

public class FileMetadata
{
    public string MimeType { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, string> Tags { get; set; } = new();
}
