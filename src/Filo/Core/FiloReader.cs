using ManuHub.Filo.Utils;
using System.Security.Cryptography;
using System.Text.Json;

namespace ManuHub.Filo;

public class FiloReader
{
    private readonly string _path;
    private List<FileEntry> _fileEntries = new();

    public FiloHeader Header { get; private set; } = default!;
    public FiloReader(string path) => _path = path;

    /// <summary>
    /// Initializes the container by reading header and index.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            await using var fs = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);

            if (fs.Length < 16)
                throw new InvalidDataException("FILO container too small or corrupted.");

            // -------- MAGIC --------
            var magicBuffer = new byte[4];
            await fs.ReadExactlyAsync(magicBuffer);

            var magic = System.Text.Encoding.ASCII.GetString(magicBuffer);

            if (magic != Filo.Magic)
                throw new InvalidDataException("Invalid FILO container.");

            // -------- VERSION --------
            var intBuffer = new byte[4];
            await fs.ReadExactlyAsync(intBuffer);
            int version = BitConverter.ToInt32(intBuffer);

            if (version > Filo.Version)
                throw new InvalidDataException("Unsupported FILO version.");

            // -------- HEADER --------
            await fs.ReadExactlyAsync(intBuffer);
            int headerLength = BitConverter.ToInt32(intBuffer);

            if (headerLength <= 0 || headerLength > 1_000_000)
                throw new InvalidDataException("Invalid header length.");

            var headerBytes = new byte[headerLength];
            await fs.ReadExactlyAsync(headerBytes);

            Header = JsonSerializer.Deserialize<FiloHeader>(headerBytes)
                ?? throw new InvalidDataException("Failed to deserialize header.");

            // -------- FOOTER --------
            fs.Seek(-16, SeekOrigin.End);

            var longBuffer = new byte[8];

            await fs.ReadExactlyAsync(longBuffer);
            long indexOffset = BitConverter.ToInt64(longBuffer);

            await fs.ReadExactlyAsync(longBuffer);
            long metadataOffset = BitConverter.ToInt64(longBuffer);

            if (indexOffset >= fs.Length)
                throw new InvalidDataException("Invalid index offset.");

            // -------- READ INDEX --------
            fs.Position = indexOffset;

            await fs.ReadExactlyAsync(intBuffer);
            int indexLen = BitConverter.ToInt32(intBuffer);

            if (indexLen <= 0 || indexLen > fs.Length - indexOffset)
                throw new InvalidDataException("Invalid index length.");

            var indexBytes = new byte[indexLen];
            await fs.ReadExactlyAsync(indexBytes);

            _fileEntries = JsonSerializer.Deserialize<List<FileEntry>>(indexBytes)
                           ?? throw new InvalidDataException("Failed to parse index.");
        }
        catch (FileNotFoundException)
        {
            Console.Error.WriteLine($"FILO file not found: {_path}");
            throw;
        }
        catch (IOException ioEx)
        {
            Console.Error.WriteLine($"IO error reading FILO container: {ioEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error initializing FILO container: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Returns all files inside the container.
    /// </summary>
    public IEnumerable<string> ListFiles() => _fileEntries.Select(f => f.FilePath);

    /// <summary>
    /// Returns file metadata entry.
    /// </summary>
    public FileEntry? GetFileEntry(string fileName) => _fileEntries.FirstOrDefault(f => f.FilePath == fileName);

    /// <summary>
    /// Derives a 256-bit AES key from password using PBKDF2.
    /// </summary>
    public byte[] DeriveKey(string password)
    {
        if (Header.Salt == null)
            throw new InvalidOperationException("Container is not password protected.");

        var salt = Convert.FromBase64String(Header.Salt);

        byte[] key = new byte[32];

        Rfc2898DeriveBytes.Pbkdf2(password, salt, key, 100_000, HashAlgorithmName.SHA256);

        return key;
    }

    /// <summary>
    /// Streams a file from the container chunk-by-chunk.
    /// </summary>
    public async IAsyncEnumerable<byte[]> StreamFileAsync(string fileName, byte[]? key = null)
    {
        var entry = _fileEntries.FirstOrDefault(f => f.FilePath == fileName)
                ?? throw new FileNotFoundException($"File '{fileName}' not found in container.");

        if (Header.Encryption == "AES256" && key == null)
            throw new InvalidOperationException("This container is encrypted. Provide a key.");

        await using var fs = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);

        foreach (var chunk in entry.Chunks)
        {
            fs.Position = chunk.Offset;

            byte[] dataChunk;

            try
            {
                if (Header.Encryption == "AES256")
                {
                    // -------- IV --------
                    var iv = new byte[16];
                    await fs.ReadExactlyAsync(iv);

                    // -------- LENGTH --------
                    var lenBuf = new byte[4];
                    await fs.ReadExactlyAsync(lenBuf);
                    int len = BitConverter.ToInt32(lenBuf);

                    if (len <= 0 || len > 100_000_000)
                        throw new InvalidDataException("Invalid encrypted chunk length.");

                    var enc = new byte[len];
                    await fs.ReadExactlyAsync(enc);

                    dataChunk = FiloEncryption.Decrypt(enc, key!, iv);
                }
                else
                {
                    // -------- LENGTH --------
                    var lenBuf = new byte[4];
                    await fs.ReadExactlyAsync(lenBuf);
                    int len = BitConverter.ToInt32(lenBuf);

                    if (len <= 0 || len > 100_000_000)
                        throw new InvalidDataException("Invalid chunk length.");

                    dataChunk = new byte[len];
                    await fs.ReadExactlyAsync(dataChunk);
                }
            }
            catch (IOException ioEx)
            {
                Console.Error.WriteLine($"IO error streaming chunk {chunk.Id} of '{fileName}': {ioEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error streaming chunk {chunk.Id} of '{fileName}': {ex.Message}");
                throw;
            }

            yield return dataChunk;  // must be outside try/catch
        }
    }

    /// <summary>
    /// Creates a stream for reading a file directly.
    /// </summary>
    public Stream OpenStream(string fileName, byte[]? key = null) => new FiloStream(this, fileName, key);
}