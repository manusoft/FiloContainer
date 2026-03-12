# FILO – Fast, Flexible, Multi-file Container for .NET

![Static Badge](https://img.shields.io/badge/FILO-blue)
![NuGet Version](https://img.shields.io/nuget/v/Filo)
![NuGet Downloads](https://img.shields.io/nuget/dt/Filo)
<p align="center"><img src="https://visitor-badge.laobi.icu/badge?page_id=manusoft/FiloContainer" style="max-width: 100%;"></p>

---

<img width="512" height="512" alt="FILO" src="https://github.com/user-attachments/assets/d26100b0-2d96-480c-80b1-3e6501ebcd33" />


## Overview

**FILO** (Files In, Layered & Organized) is a modern **multi-file container format** for .NET designed to handle **large files efficiently**.  
It stores multiple files (video, audio, text, binaries, etc.) in a **single container**, supporting:

- **Large files** (videos, audio, binaries)
- **Multiple files per container**
- Chunked streaming for **GB-sized files**
- AES256 optional encryption per chunk
- Metadata storage
- File checksums for integrity
- Fully async and memory-efficient operations

> FILO = **Files In, Layered & Organized**

It is ideal for **video/audio streaming, backup containers, and custom file packaging**.

---

## Why FILO?

Traditional ZIP or JSON-based storage has limitations:

- Limited streaming support for large files
- No native chunked encryption
- Metadata is often scattered or external

**FILO solves this** by:

- Storing files in **chunks** for streaming or partial reads  
- Supporting **encryption at chunk level**  
- Embedding **metadata and checksums**  
- Providing **simple async APIs** in fluent style  

---

## FILO Container Layout

```text
+------------------------------------------------+
| Header (JSON)                                  |
|  - Format: "FILO"                              |
|  - Version                                     |
|  - Created (UTC)                               |
|  - ChunkSize                                   |
|  - FileCount                                   |
|  - Compression                                 |
|  - Encryption                                  |
|  - Description                                 |
+------------------------------------------------+
| File Chunks                                    |
|  [chunk1] [chunk2] ...                         |
|  (Encrypted if AES256)                         |
+------------------------------------------------+
| Index (JSON)                                   |
|  - File names                                  |
|  - Offsets                                     |
|  - Chunk sizes                                 |
+------------------------------------------------+
| Metadata (JSON)                                |
|  - File metadata (MIME type, tags, etc.)       |
+------------------------------------------------+
| Checksum (JSON)                                |
|  - SHA256 hashes                               |
+------------------------------------------------+
| Footer                                         |
|  - Index offset                                |
|  - Metadata offset                             |
+------------------------------------------------+
```
> This design allows **streaming large files directly**, without full extraction.

---

## Comparison with Other Formats

| Feature                  | FILO                 | ZIP                | JSON Container   | Raw BLOB       |
| ------------------------ | -------------------- | ------------------ | ---------------- | -------------- |
| Multi-file support       | ✅ Yes                | ✅ Yes              | ❌ No             | ❌ No           |
| Streaming large files    | ✅ Yes, chunked       | ❌ Needs extraction | ❌ Needs parsing  | ❌ No           |
| Async support            | ✅ Fully async        | ❌ Limited          | ✅ Async with lib | ✅ Async        |
| Encryption               | ✅ Chunk-level AES256 | ✅ Whole file       | ❌ No native      | ✅ App-level    |
| Metadata storage         | ✅ Embedded JSON      | ❌ Limited          | ✅ Yes            | ❌ No           |
| Checksums / Integrity    | ✅ SHA256 per file    | ❌ Optional         | ❌ Needs custom   | ❌ Needs custom |
| Browser/Blazor streaming | ✅ Yes                | ❌ No               | ❌ No             | ❌ No           |

> FILO is ideal for **media, backups, and server-side streaming** where large files need chunked access.

---


## Installation

Install via NuGet:

```bash
dotnet add package Filo.1.0.0
````

---

## Basic Usage

### Writing a container

```csharp
using Filo;
using System.Security.Cryptography;

var key = RandomNumberGenerator.GetBytes(32); // AES256 key

var writer = new FiloWriter("backup.filo")
    .AddFile("video.mp4", new FileMetadata { MimeType = "video/mp4" })
    .AddFile("subtitle.srt", new FileMetadata { MimeType = "text/plain" })
    .WithChunkSize(5_000_000)
    .WithEncryption(key);

await writer.WriteAsync();
Console.WriteLine("FILO container created!");
```

### Reading files

```csharp
var reader = new FiloReader("backup.filo");
await reader.InitializeAsync();

// List files in container
foreach (var file in reader.ListFiles())
    Console.WriteLine(file);

// Stream a file (AES256 encrypted example)
await foreach (var chunk in reader.StreamFileAsync("video.mp4", key))
{
    // Process chunk
}
```

---

## Streaming Video/Audio

FILO supports **direct streaming without reassembling the file**:

```csharp
await using var filoStream = new FiloStream(reader, "video.mp4", key);
await using var output = new FileStream("video_streamed.mp4", FileMode.Create);
await filoStream.CopyToAsync(output);
```

### In Blazor Server / ASP.NET Core

```csharp
[HttpGet("video/{fileName}")]
public async Task<IActionResult> StreamVideo(string fileName)
{
    var reader = new FiloReader("backup.filo");
    await reader.InitializeAsync();
    var filoStream = new FiloStream(reader, fileName, key: YourKeyHere);

    return File(filoStream, "video/mp4", enableRangeProcessing: true);
}
```

* Supports **large files**, **streaming**, and **AES256 encrypted chunks**
* Browser can **seek**, **pause**, and **resume** seamlessly

---

## Multi-file container

You can store multiple files in the same container:

```csharp
var writer = new FiloWriter("media.filo")
    .AddFile("movie.mp4", new FileMetadata { MimeType = "video/mp4" })
    .AddFile("audio.mp3", new FileMetadata { MimeType = "audio/mpeg" })
    .AddFile("subtitle.srt", new FileMetadata { MimeType = "text/plain" })
    .WithChunkSize(10_000_000)
    .WithEncryption(key);

await writer.WriteAsync();
```

* The container will store **indexes, metadata, and checksums**.
* You can **stream each file individually** using `FiloStream` or `StreamFileAsync`.

---

## Chunked Streaming

* FILO reads files in **chunks** to minimize memory usage.
* Suitable for **large video/audio files**.
* Supports **AES256 encryption per chunk**.

```csharp
await foreach (var chunk in reader.StreamFileAsync("largevideo.mp4", key))
{
    // Process chunk (send to player or API)
}
```

> Always verify checksum for **large file integrity**.

---

## Checksums & Integrity

FILO stores **SHA256 checksums** for each file:

```csharp
var checksum = await FiloChecksum.ComputeFileSHA256Async("video.mp4");
Console.WriteLine(checksum);
```

You can verify that **streamed files match the original**.

---

## Fluent API Summary

| Class              | Key Methods                                                            |
| ------------------ | ---------------------------------------------------------------------- |
| `FiloWriter`       | `.AddFile()`, `.WithChunkSize()`, `.WithEncryption()`, `.WriteAsync()` |
| `FiloReader`       | `.InitializeAsync()`, `.ListFiles()`, `.StreamFileAsync()`             |
| `FiloStream`       | `.ReadAsync()` – supports streaming directly to players                |
| `FiloChecksum`     | `.ComputeFileSHA256Async()`, `.ComputeFileSHA256Async()`, `.ComputeSHA256()`, `.Verify()`,`.VerifyFileAsync()` |
| `FiloEncryption`   | `.Encrypt()`, `.Decrypt()`                                             |

---

## Notes

* FILO supports **any file type**: video, audio, images, text, binaries
* For **large containers (GBs)**, keep them **server-side** and stream with `FiloStream`.
* Fully **async and memory-efficient**

---

## License

MIT License



