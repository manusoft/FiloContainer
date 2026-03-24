namespace ManuHub.Filo;

public class FiloChunkIndex
{
    public int Id { get; set; }
    public long Offset { get; set; }
    public int Length { get; set; }
    public string? Hash { get; set; } // v1.1
}