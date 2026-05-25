using ManuHub.Filo.Utils;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ManuHub.Filo;

public class FiloReader
{
    private readonly string _path;

    private List<FileEntry> _fileEntries = new();
    private Dictionary<string, FileEntry> _fileMap = new(StringComparer.OrdinalIgnoreCase);

    public FiloHeader Header { get; private set; } = default!;
    public FiloReader(string path) => _path = path;

    /// <summary>
    /// Initializes the container by reading header and index.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            await using var fs = new FileStream(
                _path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                128 * 1024,
                FileOptions.SequentialScan);

            if (fs.Length < 16)
                throw new InvalidDataException("FILO container too small or corrupted.");

            // MAGIC
            var magicBuffer = new byte[4];
            await fs.ReadExactlyAsync(magicBuffer);

            var magic = Encoding.ASCII.GetString(magicBuffer);

            if (magic != Filo.Magic)
                throw new InvalidDataException("Invalid FILO container.");

            // VERSION
            var intBuffer = new byte[4];
            await fs.ReadExactlyAsync(intBuffer);

            int version = BitConverter.ToInt32(intBuffer);

            if (version > Filo.Version)
                throw new InvalidDataException("Unsupported FILO version.");

            // HEADER LENGTH
            await fs.ReadExactlyAsync(intBuffer);

            int headerLength = BitConverter.ToInt32(intBuffer);

            if (headerLength <= 0 || headerLength > 1_000_000)
                throw new InvalidDataException("Invalid header length.");

            // HEADER
            var headerBytes = new byte[headerLength];
            await fs.ReadExactlyAsync(headerBytes);

            Header = JsonSerializer.Deserialize<FiloHeader>(headerBytes)
                ?? throw new InvalidDataException("Failed to deserialize header.");

            // FOOTER
            fs.Seek(-16, SeekOrigin.End);

            var longBuffer = new byte[8];

            await fs.ReadExactlyAsync(longBuffer);
            long indexOffset = BitConverter.ToInt64(longBuffer);

            await fs.ReadExactlyAsync(longBuffer);
            long metadataOffset = BitConverter.ToInt64(longBuffer);

            if (indexOffset >= fs.Length)
                throw new InvalidDataException("Invalid index offset.");

            // READ INDEX
            fs.Position = indexOffset;

            await fs.ReadExactlyAsync(intBuffer);
            int indexLen = BitConverter.ToInt32(intBuffer);

            if (indexLen <= 0 || indexLen > fs.Length - indexOffset)
                throw new InvalidDataException("Invalid index length.");

            var indexBytes = new byte[indexLen];
            await fs.ReadExactlyAsync(indexBytes);

            _fileEntries = JsonSerializer.Deserialize<List<FileEntry>>(indexBytes)
                           ?? throw new InvalidDataException("Failed to parse index.");

            // FAST LOOKUP MAP
            _fileMap = _fileEntries.ToDictionary(f => f.Path, StringComparer.OrdinalIgnoreCase);
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

    public bool FileExists(string path)
    {
        return _fileMap.ContainsKey(path);
    }

    /// <summary>
    /// Returns file inside the container.
    /// </summary>
    public FiloFileInfo? GetFileInfo(string path)
    {
        if (!_fileMap.TryGetValue(path, out var entry))
            return null;

        if (entry == null)
            return null;

        return new FiloFileInfo
        {
            Name = Path.GetFileName(path),
            Path = entry.Path,
            MimeType = entry.MimeType,
            FileSize = entry.FileSize,
            ChunkCount = entry.Chunks.Count,
            Encrypted = entry.Encrypted
        };
    }

    /// <summary>
    /// Returns all files inside the container.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<FiloFileInfo> ListFiles()
    {
        return _fileEntries.Select(e => new FiloFileInfo
        {
            Name = Path.GetFileName(e.Path),
            Path = e.Path,
            MimeType = e.MimeType,
            FileSize = e.FileSize,
            ChunkCount = e.Chunks.Count,
            Encrypted = e.Encrypted
        }).ToList();
    }

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

        if (Header.PasswordCheck != null)
        {
            var check = SHA256.HashData(key);

            if (!check.SequenceEqual(Header.PasswordCheck))
                throw new CryptographicException("Invalid password.");
        }

        return key;
    }

    /// <summary>
    /// Streams a file from the container chunk-by-chunk.
    /// </summary>
    public async IAsyncEnumerable<byte[]> StreamFileAsync(string fileName, byte[]? key = null)
    {
        if (!_fileMap.TryGetValue(fileName, out var entry))
            throw new FileNotFoundException($"File '{fileName}' not found in container.");

        if (Header.Encryption == "AES256" && Header.EncryptionMode != "AES-CBC")
            throw new InvalidDataException("Unsupported encryption mode.");

        if (entry.Chunks.Count == 0)
            yield break;

        await using var fs = new FileStream(
            _path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            128 * 1024,
            FileOptions.SequentialScan);

        foreach (var chunk in entry.Chunks)
        {
            fs.Position = chunk.Offset;

            byte[] dataChunk;

            try
            {
                if (Header.Encryption == "AES256")
                {
                    var iv = new byte[16];
                    await fs.ReadExactlyAsync(iv);

                    var lenBuf = new byte[4];
                    await fs.ReadExactlyAsync(lenBuf);
                    int len = BitConverter.ToInt32(lenBuf);

                    var enc = new byte[len];
                    await fs.ReadExactlyAsync(enc);

                    dataChunk = FiloEncryption.Decrypt(enc, key!, iv);
                }
                else
                {
                    var lenBuf = new byte[4];
                    await fs.ReadExactlyAsync(lenBuf);

                    int len = BitConverter.ToInt32(lenBuf);

                    dataChunk = new byte[len];
                    await fs.ReadExactlyAsync(dataChunk);
                }

                // INTEGRITY CHECK
                var computed = Convert.ToHexString(SHA256.HashData(dataChunk));

                if (!string.Equals(computed, chunk.Hash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Chunk {chunk.Id} failed integrity check.");
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

    public async Task ExtractFileAsync(string path, string outputPath, byte[]? key = null)
    {
        using var stream = OpenStream(path, key);
        using var output = File.Create(outputPath);

        await stream.CopyToAsync(output);
    }

    public async Task ExtractDirectoryAsync(string directory, string outputFolder, byte[]? key = null)
    {
        Directory.CreateDirectory(outputFolder);

        var files = ListFiles()
            .Where(f => f.Path.StartsWith(directory, StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            var relative = file.Path.Substring(directory.Length).TrimStart('/');

            var outputPath = Path.Combine(outputFolder, relative);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            await using var input = new FiloStream(this, file.Path, key);
            await using var output = new FileStream(outputPath, FileMode.Create);

            await input.CopyToAsync(output);
        }
    }

    public static async Task<FiloHeader> ReadHeaderAsync(string path)
    {
        await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);

        var magic = new byte[4];
        await fs.ReadExactlyAsync(magic);

        var intBuf = new byte[4];

        await fs.ReadExactlyAsync(intBuf); // version

        await fs.ReadExactlyAsync(intBuf);
        int headerLen = BitConverter.ToInt32(intBuf);

        var headerBytes = new byte[headerLen];
        await fs.ReadExactlyAsync(headerBytes);

        return JsonSerializer.Deserialize<FiloHeader>(headerBytes)!;
    }
}