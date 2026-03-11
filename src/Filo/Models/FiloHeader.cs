namespace ManuHub.Filo;

public class FiloHeader
{
    /// <summary>
    /// Container format name
    /// </summary>
    public string Format { get; set; } = Filo.Magic;

    /// <summary>
    /// Format version
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// When the container was created
    /// </summary>
    public DateTime Created { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Chunk size used for binary storage
    /// </summary>
    public int ChunkSize { get; set; }

    /// <summary>
    /// Number of files stored in the container
    /// </summary>
    public int FileCount { get; set; } = 0; 

    /// <summary>
    /// Compression algorithm (none, gzip, zstd etc.)
    /// </summary>
    public string Compression { get; set; } = "none";

    /// <summary>
    /// Encryption algorithm (none, aes256 etc.)
    /// </summary>
    public string Encryption { get; set; } = "none";

    /// <summary>
    /// Optional container description
    /// </summary>
    public string? Description { get; set; }
}