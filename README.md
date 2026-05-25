# FILO – Fast, Flexible, Multi-file Container for .NET

![Static Badge](https://img.shields.io/badge/FILO-blue)
![NuGet Version](https://img.shields.io/nuget/v/Filo)
![NuGet Downloads](https://img.shields.io/nuget/dt/Filo)
![Visitors](https://visitor-badge.laobi.icu/badge?page_id=manusoft/FiloContainer)

<img width="512" height="512" alt="FILO" src="https://github.com/user-attachments/assets/d26100b0-2d96-480c-80b1-3e6501ebcd33" />

<img width="1089" height="819" alt="image" src="https://github.com/user-attachments/assets/464971f7-9318-4b64-8edf-d27f1d485341" />
<img width="1089" height="819" alt="image" src="https://github.com/user-attachments/assets/d41d9a3e-ce46-4007-8de1-4f3f1c9a3cfe" />
<img width="1089" height="819" alt="image" src="https://github.com/user-attachments/assets/249e4784-83eb-4902-8921-1081e7495699" />


---

## FILO v1.2.0 Highlights

### 🧱 Stability & Format Fixes
- ✔ Fixed footer structure (v1.2 deterministic format)
- ✔ Standardized chunk format (encrypted + plain)
- ✔ Fixed index offset validation issues
- ✔ Added footer magic validation (`FLOF`)
- ✔ Stronger container corruption detection

### 🔐 Security Improvements
- ✔ Password-based encryption (PBKDF2 + AES)
- ✔ Encryption contract stabilized (AES-CBC defined)
- ✔ Password verification via SHA256 check
- ✔ Safer chunk validation during streaming

### ⚙️ Reliability Improvements
- ✔ Safe offset & length validation
- ✔ Stronger reader error handling
- ✔ Deterministic file structure

---

## Overview

**FILO** (Files In, Layered & Organized) is a modern multi-file container format for .NET designed for large-scale file storage and streaming.

It supports:

- Large files (GB-sized video/audio/binaries)
- Multi-file containers
- Chunked streaming (memory efficient)
- Optional AES256 encryption per chunk
- Embedded metadata & integrity checks
- Fully async APIs

> FILO is designed for **streaming-first storage systems**, not just archive compression.

---

## Why FILO?

Traditional formats like ZIP have limitations:

- ❌ Poor streaming support
- ❌ Weak chunk-level control
- ❌ No streaming encryption model
- ❌ Limited metadata structure

FILO solves this by:

- Streaming files in **chunks**
- Encrypting **per chunk**
- Embedding **structured metadata**
- Supporting **direct streaming without extraction**

---

## FILO Container Layout (v1.2)

```text
+------------------------------------------------+
| HEADER (JSON)                                  |
|------------------------------------------------|
| - Format: FILO                                 |
| - Version: 1.2                                 |
| - ChunkSize                                    |
| - FileCount                                    |
| - Encryption Mode (AES-CBC)                    |
| - KDF (PBKDF2)                                 |
+------------------------------------------------+
| FILE CHUNKS                                    |
|  [IV][LEN][DATA] (encrypted)                   |
|  [LEN][DATA] (plain)                           |
+------------------------------------------------+
| INDEX (JSON)                                   |
+------------------------------------------------+
| METADATA (JSON)                                |
+------------------------------------------------+
| CHECKSUM (JSON)                                |
+------------------------------------------------+
| FOOTER                                         |
| - IndexOffset                                  |
| - MetadataOffset                               |
| - ChecksumOffset                               |
| - "FLOF" magic                                 |
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
dotnet add package Filo --version 1.2.0
````

---

## Basic Usage

### 📦 Create Container

```csharp
using Filo;

var writer = new FiloWriter("backup.filo")
    .AddFile("video.mp4", new FileMetadata { MimeType = "video/mp4" })
    .AddFile("audio.mp3", new FileMetadata { MimeType = "audio/mpeg" })
    .WithChunkSize(5_000_000)
    .WithPassword("1234567890");

await writer.WriteAsync();

Console.WriteLine("FILO container created!");
```

### 📖 Read Container

```csharp
var reader = new FiloReader("backup.filo");
await reader.InitializeAsync();

var key = reader.DeriveKey("1234567890");

foreach (var file in reader.ListFiles())
{
    Console.WriteLine($"{file.Name} ({file.FileSize} bytes)");
}
```

---
### 📡 Stream File (Recommended)

```csharp
await using var stream = reader.OpenStream("video.mp4", key);
await using var output = File.Create("restored.mp4");

await stream.CopyToAsync(output);
```

---

### 🔁 Chunk-by-chunk processing

```csharp
await foreach (var chunk in reader.StreamFileAsync("video.mp4", key))
{
    // Process streaming data
}
```

---

### 🔐 Encryption Model (v1.2)

- Encrypted chunk format:
    ```
    [16-byte IV][4-byte length][encrypted data]
    ```
- Plain chunk format:
    ```
    [4-byte length][plain data]
    ```

* Key derived using PBKDF2 (100k iterations)
* AES-CBC encryption per chunk
* Password verification via SHA256 key hash

---

### 🧠 Integrity System

- Each chunk is validated using SHA256:
    ```csharp
    var checksum = await FiloChecksum.ComputeFileSHA256Async("video.mp4");
    ```

* Prevents corrupted chunk playback
* Ensures file integrity during streaming

---

### 🌐 ASP.NET Streaming Example

```csharp
public async Task<IActionResult> GetVideo()
{
    var reader = new FiloReader("media.filo");
    await reader.InitializeAsync();

    var key = reader.DeriveKey("password");

    var stream = new FiloStream(reader, "movie.mp4", key);

    return File(stream, "video/mp4");
}
```

> Supports **large files**, **streaming**, and **AES256 encrypted chunks**. Browser can **seek, pause, and resume** seamlessly.

---

## Multi-file Container Example

```csharp
var writer = new FiloWriter("media.filo")
    .AddFile("movie.mp4", new FileMetadata { MimeType = "video/mp4" })
    .AddFile("audio.mp3", new FileMetadata { MimeType = "audio/mpeg" })
    .AddFile("subtitle.srt", new FileMetadata { MimeType = "text/plain" })
    .WithChunkSize(10_000_000)
    .WithPassword("mypassword");

await writer.WriteAsync();
```

* Stores **indexes, metadata, and checksums**
* Stream **each file individually** using `FiloStream` or `StreamFileAsync`

---

## Chunked Streaming

* Reads files in **memory-efficient chunks**
* Ideal for **large video/audio files**
* Supports **AES256 encryption per chunk**

```csharp
await foreach (var chunk in reader.StreamFileAsync("largevideo.mp4", key))
{
    // Process chunk (send to player or API)
}
```

---

## ⚡ When to Use What

| Method              | Use Case                |
| ------------------- | ----------------------- |
| `OpenStream()`      | Direct file streaming   |
| `FiloStream`        | ASP.NET / UI streaming  |
| `StreamFileAsync()` | Custom chunk processing |
| `CopyToAsync()`     | Extraction              |


---

### 📦 File Metadata

```csharp
new FileMetadata
{
    MimeType = "video/mp4",
    Description = "Main movie file"
}
```
---

> Always verify checksum for **large file integrity**.


## Checksums & Integrity

```csharp
var checksum = await FiloChecksum.ComputeFileSHA256Async("video.mp4");
Console.WriteLine(checksum);
```

* Ensures **streamed files match the original**

---

## Fluent API Summary

| Class              | Key Methods                                                            |
| ------------------ | ---------------------------------------------------------------------- |
| `FiloWriter`       | `.AddFile()`, `AddDirectory()`, `.WithChunkSize()`, `.WithPassword()`, `.WriteAsync()` |
| `FiloReader`       | `.InitializeAsync()`, `DeriveKey()`, `FileExists()`, `GetFileInfo()`, `.ListFiles()`, `.StreamFileAsync()`, `OpenStream()`, `ExtractFileAsync()`, `ExtractDirectoryAsync()`, `ReadHeaderAsync()`            |
| `FiloStream`       | `.ReadAsync()` – supports streaming directly to players, `Read()`                |
| `FiloChecksum`     | `.ComputeSHA256()`, `.ComputeSHA256Async()`, `.ComputeFileSHA256Async()`, `.ComputeFileSHA256Async()`,`.Verify()`, `VerifyFileAsync()` |
| `FiloEncryption`   | `.Encrypt()`, `.Decrypt()`                                             |

---

## 🔧 Core Classes

| Class          | Responsibility         |
| -------------- | ---------------------- |
| FiloWriter     | Builds container       |
| FiloReader     | Reads container        |
| FiloStream     | Streaming abstraction  |
| FiloChecksum   | Integrity verification |
| FiloEncryption | AES operations         |

---


## Notes (v1.2 Rules)

* Footer size is fixed (28 bytes)
* Chunk offsets always point to chunk start
* AES-CBC is the defined encryption mode
* Index must always validate against file length
* Footer magic must be "FLOF"

---

## License

MIT License


