using ManuHub.Filo;
using System.Security.Cryptography;

string filoPath = "backupv1.1.filo";

try
{
    // Create container
    var writer = new FiloWriter(filoPath)
        .AddFile("C:\\Users\\manua\\Videos\\anu.mp4", new FileMetadata { MimeType = "video/mp4" })
        .AddFile("C:\\Users\\manua\\Videos\\anu_kavya.mp4", new FileMetadata { MimeType = "video/mp4" })
        .WithChunkSize(5_000_000)
        .WithPassword("1234567890");

    await writer.WriteAsync();
    Console.WriteLine("FILO container written!");
}
catch (CryptographicException cex)
{
    Console.WriteLine(cex.Message);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

try
{
    // Read container
    var reader = new FiloReader(filoPath);
    await reader.InitializeAsync();

    var key = reader.DeriveKey("1234567890");

    Console.WriteLine("Files in container:");
    // List files in container
    foreach (var file in reader.ListFiles())
        Console.WriteLine(file);

    // Reassemble
    foreach (var f in reader.ListFiles())
    {
        string outFile = $"restored_{f}";
        await using var filoStream = new FiloStream(reader, f, key);
        await using var output = new FileStream(outFile, FileMode.Create);
        await filoStream.CopyToAsync(output);
    }

    Console.WriteLine("All files reassembled successfully!");
}
catch(CryptographicException cex)
{
    Console.WriteLine(cex.Message);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}