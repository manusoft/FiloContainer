using ManuHub.Filo;

namespace Filo.Test;

public class FiloTests
{
    [Fact]
    public async Task WriteAndReadFiloContainer()
    {
        var writer = new FiloWriter("test.filo")
            .AddFile("example.txt", new FileMetadata { MimeType = "text/plain" })
            .WithChunkSize(1024 * 1024);

        await writer.WriteAsync();

        var reader = new FiloReader("test.filo");
        await reader.InitializeAsync();

        var files = reader.ListFiles();
        Assert.Contains("example.txt", files);
    }
}
