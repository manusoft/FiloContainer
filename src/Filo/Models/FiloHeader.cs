namespace ManuHub.Filo;

public class FiloHeader
{
    public string Format { get; set; } = Filo.Magic;
    public int Version { get; set; } = 1;
    public DateTime Created { get; set; } = DateTime.UtcNow;

    public int ChunkSize { get; set; }

    public int FileCount { get; set; } = 0;

    public string Compression { get; set; } = "none";

    public string Encryption { get; set; } = "none";

    public string? EncryptionMode { get; set; } = "AES-CBC";

    public string? Kdf { get; set; } // v1.1

    public string? Salt { get; set; } // v1.1

    public byte[]? PasswordCheck { get; set; }

    public string? Description { get; set; }
}