using System;
using System.Collections.Generic;
using System.IO;

namespace FiloExplorer.Helpers;

public static class MimeHelper
{
    private static readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".bmp"] = "image/bmp",
        [".webp"] = "image/webp",

        // Video
        [".mp4"] = "video/mp4",
        [".mkv"] = "video/x-matroska",
        [".webm"] = "video/webm",
        [".avi"] = "video/x-msvideo",

        // Audio
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".aac"] = "audio/aac",
        [".flac"] = "audio/flac",

        // Text
        [".txt"] = "text/plain",
        [".json"] = "application/json",
        [".xml"] = "application/xml",
        [".html"] = "text/html",
        [".css"] = "text/css",

        // Other
        [".pdf"] = "application/pdf",
        [".zip"] = "application/zip",
    };

    public static string GetMimeType(string filePath)
    {
        var ext = Path.GetExtension(filePath);

        if (!string.IsNullOrEmpty(ext) && _map.TryGetValue(ext, out var mime))
            return mime;

        return "application/octet-stream"; // fallback
    }
}
