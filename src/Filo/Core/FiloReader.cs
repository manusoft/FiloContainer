using ManuHub.Filo.Utils;
using System.Text.Json;

namespace ManuHub.Filo;

public class FiloReader
{
    private readonly string _path;
    private List<FileEntry> _fileEntries = new();

    public FiloReader(string path) => _path = path;

    /// <summary>
    /// Reads the container and initializes index and metadata.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            await using var fs = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);

            if (fs.Length < 16)
                throw new InvalidDataException("FILO container too small or corrupted.");

            // Read footer for indexOffset
            fs.Seek(-16, SeekOrigin.End);
            var longBuffer = new byte[8];
            await fs.ReadExactlyAsync(longBuffer);
            long indexOffset = BitConverter.ToInt64(longBuffer);

            await fs.ReadExactlyAsync(longBuffer);
            long metadataOffset = BitConverter.ToInt64(longBuffer);

            if (indexOffset >= fs.Length || metadataOffset >= fs.Length)
                throw new InvalidDataException("FILO container footer offsets are invalid.");

            // Read index
            fs.Position = indexOffset;
            var intBuffer = new byte[4];
            await fs.ReadExactlyAsync(intBuffer);
            int indexLen = BitConverter.ToInt32(intBuffer);

            if (indexLen <= 0 || indexLen > fs.Length - indexOffset)
                throw new InvalidDataException("FILO container index length is invalid.");

            var indexBytes = new byte[indexLen];
            await fs.ReadExactlyAsync(indexBytes);
            _fileEntries = JsonSerializer.Deserialize<List<FileEntry>>(indexBytes)
                           ?? throw new InvalidDataException("Failed to deserialize file index.");
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
    /// Lists all files contained in the FILO container.
    /// </summary>
    public IEnumerable<string> ListFiles() => _fileEntries.Select(f => f.FileName);


    /// <summary>
    /// Lists all files contained in the FILO container.
    /// </summary>
    public async IAsyncEnumerable<byte[]> StreamFileAsync(string fileName, byte[]? key = null)
    {
        var entry = _fileEntries.FirstOrDefault(f => f.FileName == fileName)
                ?? throw new FileNotFoundException($"File '{fileName}' not found in container.");

        await using var fs = new FileStream(_path, FileMode.Open, FileAccess.Read);

        foreach (var chunk in entry.Chunks)
        {
            fs.Position = chunk.Offset;

            byte[] dataChunk;

            try
            {
                if (key != null)
                {
                    // Encrypted chunk
                    var iv = new byte[16];
                    await fs.ReadExactlyAsync(iv);

                    var lenBuf = new byte[4];
                    await fs.ReadExactlyAsync(lenBuf);
                    int len = BitConverter.ToInt32(lenBuf);

                    var enc = new byte[len];
                    await fs.ReadExactlyAsync(enc);

                    dataChunk = FiloEncryption.Decrypt(enc, key, iv);
                }
                else
                {
                    // Plain chunk
                    var lenBuf = new byte[4];
                    await fs.ReadExactlyAsync(lenBuf);
                    int len = BitConverter.ToInt32(lenBuf);

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
}