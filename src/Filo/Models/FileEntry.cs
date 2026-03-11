namespace ManuHub.Filo;

public class FileEntry
{
    public string FileName { get; set; } = "";
    public string MimeType { get; set; } = "";
    public long FileSize { get; set; }
    public List<FiloChunkIndex> Chunks { get; set; } = new();
}