using ManuHub.Filo.Utils;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ManuHub.Filo;

public class FiloWriter
{
    private readonly string _outputPath;
    private readonly List<(string path, FileMetadata meta)> _files = new();
    private int _chunkSize = 10_000_000;
    private bool _encrypt;
    private string? _password;
    private byte[]? _key = new byte[32];
    private byte[]? _salt;

    public FiloWriter(string outputPath) => _outputPath = outputPath;

    public FiloWriter AddFile(string filePath, FileMetadata metadata)
    {
        _files.Add((filePath, metadata));
        return this;
    }

    public FiloWriter WithChunkSize(int chunkSize)
    {
        _chunkSize = chunkSize;
        return this;
    }

    public FiloWriter WithPassword(string password)
    {
        _encrypt = true;
        _password = password;
        return this;
    }

    [Obsolete("Use WithPassword(string password) instead. Raw key support will be removed in FILO v2.0.")]
    public FiloWriter WithEncryption(byte[] key)
    {
        _encrypt = true;
        _key = key;
        return this;
    }
       
    public async Task WriteAsync()
    {
        if (_files.Count == 0)
            throw new InvalidOperationException("No files added to the container.");

        if (_encrypt && _password != null)
        {
            DeriveKeyFromPassword();
        }

        try
        {
            await using var output = new FileStream(_outputPath, FileMode.Create);

            // MAGIC
            await output.WriteAsync(Encoding.ASCII.GetBytes(Filo.Magic));

            // VERSION
            await output.WriteAsync(BitConverter.GetBytes(Filo.Version));

            // HEADER
            var header = new FiloHeader
            {
                Format = Filo.Magic,
                Version = Filo.Version,
                Created = DateTime.UtcNow,
                ChunkSize = _chunkSize,
                FileCount = _files.Count,
                Compression = "none",
                Encryption = _encrypt ? "AES256" : "none",
                Kdf = !string.IsNullOrWhiteSpace(_password) ? "PBKDF2" : null,
                Salt = !string.IsNullOrWhiteSpace(_password) ? Convert.ToBase64String(_salt!) : null,
                Description = "FILO multi-file container"
            };

            var headerJson = JsonSerializer.Serialize(header);
            var headerBytes = Encoding.UTF8.GetBytes(headerJson);

            await output.WriteAsync(BitConverter.GetBytes(headerBytes.Length));
            await output.WriteAsync(headerBytes);

            // FILES
            var fileEntries = new List<FileEntry>();

            foreach (var (filePath, meta) in _files)
            {
                var entry = new FileEntry
                {
                    FileName = Path.GetFileName(filePath),
                    MimeType = meta.MimeType,
                    FileSize = new FileInfo(filePath).Length
                };

                try
                {

                    await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    var buffer = new byte[_chunkSize];
                    int read;
                    int chunkId = 0;

                    while ((read = await fs.ReadAsync(buffer)) > 0)
                    {
                        var offset = output.Position;
                        byte[] chunk = buffer[..read];

                        string hash = Convert.ToHexString(SHA256.HashData(chunk));

                        if (_encrypt)
                        {
                            var iv = RandomNumberGenerator.GetBytes(16);
                            var encrypted = FiloEncryption.Encrypt(chunk, _key!, iv);

                            await output.WriteAsync(iv);
                            await output.WriteAsync(BitConverter.GetBytes(encrypted.Length));
                            await output.WriteAsync(encrypted);

                            entry.Chunks.Add(new FiloChunkIndex
                            {
                                Id = chunkId++,
                                Offset = offset,
                                Length = encrypted.Length,
                                Hash = hash
                            });
                        }
                        else
                        {
                            await output.WriteAsync(BitConverter.GetBytes(read));
                            await output.WriteAsync(chunk);

                            entry.Chunks.Add(new FiloChunkIndex
                            {
                                Id = chunkId++,
                                Offset = offset,
                                Length = read,
                                Hash = hash
                            });
                        }
                    }
                }
                catch (IOException ioEx)
                {
                    Console.Error.WriteLine($"Error reading file '{filePath}': {ioEx.Message}");
                    throw;
                }

                fileEntries.Add(entry);
            }

            // INDEX
            var indexOffset = output.Position;
            var indexJson = JsonSerializer.Serialize(fileEntries);
            var indexBytes = Encoding.UTF8.GetBytes(indexJson);
            await output.WriteAsync(BitConverter.GetBytes(indexBytes.Length));
            await output.WriteAsync(indexBytes);

            // METADATA block (placeholder)
            var metadataOffset = output.Position;
            var metaBytes = Encoding.UTF8.GetBytes("{}");
            await output.WriteAsync(BitConverter.GetBytes(metaBytes.Length));
            await output.WriteAsync(metaBytes);

            // CHECKSUM block (placeholder)
            var checksumBytes = Encoding.UTF8.GetBytes("{}");
            await output.WriteAsync(BitConverter.GetBytes(checksumBytes.Length));
            await output.WriteAsync(checksumBytes);

            // FOOTER
            await output.WriteAsync(BitConverter.GetBytes(indexOffset));
            await output.WriteAsync(BitConverter.GetBytes(metadataOffset));

        }
        catch (UnauthorizedAccessException uaEx)
        {
            Console.Error.WriteLine($"Access denied writing container '{_outputPath}': {uaEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error during FILO WriteAsync: {ex.Message}");
            throw;
        }
    }

    private void DeriveKeyFromPassword()
    {
        _salt = RandomNumberGenerator.GetBytes(32);
        _key = new byte[32];
        Rfc2898DeriveBytes.Pbkdf2(_password!, _salt, _key, 100_000, HashAlgorithmName.SHA256);
    }
}