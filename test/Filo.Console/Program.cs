using ManuHub.Filo;
using System.Security.Cryptography;

string filoPath = "backupv1.1.filo";

try
{
    Console.WriteLine("Create new filo container:");
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
    Console.WriteLine($"CRYPTO ERROR: {cex.Message}");
    return;
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR:{ex.Message}");
    return;
}

try
{
    Console.WriteLine("Read file header:");
    var header = await FiloReader.ReadHeaderAsync("backupv1.1.filo");

    Console.WriteLine($"Files: {header.FileCount}");
    Console.WriteLine($"Created: {header.Created}");
    Console.WriteLine();

    // Read container
    var reader = new FiloReader(filoPath);
    await reader.InitializeAsync();
    var key = reader.DeriveKey("1234567890");

    Console.WriteLine("Files in container:");
    // List files in container
    foreach (var file in reader.ListFiles())
        Console.WriteLine($"{file.Name} ({file.FileSize} bytes)");

    // Reassemble
    foreach (var f in reader.ListFiles())
    {
        string outFile = $"restored_{f.Name}";
        await using var filoStream = new FiloStream(reader, f.Path, key);
        await using var output = new FileStream(outFile, FileMode.Create);
        await filoStream.CopyToAsync(output);
    }

    Console.WriteLine("All files reassembled successfully!");
}
catch (CryptographicException cex)
{
    Console.WriteLine($"CRYPTO ERROR: {cex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR:{ex.Message}");
}