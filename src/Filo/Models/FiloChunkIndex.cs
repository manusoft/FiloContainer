namespace ManuHub.Filo;

public class FiloChunkIndex
{
    /// <summary>
    /// Chunk identifier (sequential per file)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// File offset where this chunk starts.
    /// 
    /// IMPORTANT (v1.2 RULE):
    /// Offset always points to the beginning of the chunk header:
    /// 
    /// If encrypted:
    /// [IV (16 bytes)][Length (4 bytes)][Data]
    /// 
    /// If not encrypted:
    /// [Length (4 bytes)][Data]
    /// </summary>
    public long Offset { get; set; }

    /// <summary>
    /// Total stored chunk size in container (header + data)
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// SHA256 hash of ORIGINAL (decrypted) chunk data
    /// </summary>
    public string? Hash { get; set; } // v1.1
}