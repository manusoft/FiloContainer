namespace ManuHub.Filo;

public class FileList
{
    public string FileName { get; set; } = "";
    public string MimeType { get; set; } = "";
    public long FileSize { get; set; }
    public bool Encrypted { get; set; }
}