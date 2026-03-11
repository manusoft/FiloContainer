using ManuHub.Filo;
using System.Security.Cryptography;

string filoPath = "backup.filo";
byte[] key = RandomNumberGenerator.GetBytes(32); // AES256 key

// Create container
var writer = new FiloWriter(filoPath)
    .AddFile("C:\\Users\\manua\\Videos\\anu.mp4", new FileMetadata { MimeType = "video/mp4" })
    .AddFile("C:\\Users\\manua\\Videos\\numb.mp4", new FileMetadata { MimeType = "video/mp4" })
    .AddFile("C:\\Users\\manua\\Videos\\psy.mp4", new FileMetadata { MimeType = "video/mp4" })
    .AddFile("C:\\Users\\manua\\Videos\\rick.mp4", new FileMetadata { MimeType = "video/mp4" })
    .WithChunkSize(5_000_000);
    //.WithEncryption(key);

await writer.WriteAsync();
Console.WriteLine("FILO container written!");

// Read container
var reader = new FiloReader(filoPath);
await reader.InitializeAsync();

Console.WriteLine("Files in container:");
foreach (var f in reader.ListFiles()) Console.WriteLine(f);

// Reassemble
foreach (var f in reader.ListFiles())
{
    string outFile = $"restored_{f}";
    await using var filoStream = new FiloStream(reader, f);
    await using var output = new FileStream(outFile, FileMode.Create);
    await filoStream.CopyToAsync(output);
}

Console.WriteLine("All files reassembled successfully!");