namespace ManuHub.Filo;

public class FiloFileInfo
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string MimeType { get; set; } = "";
    public long FileSize { get; set; }
    public int ChunkCount { get; set; }
    public bool Encrypted { get; set; }

    public string Directory => System.IO.Path.GetDirectoryName(Path)?.Replace('\\', '/') ?? "";
}