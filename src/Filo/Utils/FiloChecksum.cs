using System.Security.Cryptography;

namespace ManuHub.Filo.Utils;

public static class FiloChecksum
{
    // Compute SHA256 from a byte array
    public static string ComputeSHA256(byte[] data)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(data);
        return Convert.ToHexString(hash);
    }

    // Compute SHA256 from a Stream (recommended for large files)
    public static async Task<string> ComputeSHA256Async(Stream stream)
    {
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream);
        return Convert.ToHexString(hash);
    }

    // Compute SHA256 from a file path
    public static async Task<string> ComputeFileSHA256Async(string filePath)
    {
        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return await ComputeSHA256Async(fs);
    }

    // Verify hash
    public static bool Verify(string expectedHash, string actualHash)
    {
        return string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase);
    }

    // Verify file integrity
    public static async Task<bool> VerifyFileAsync(string filePath, string expectedHash)
    {
        var actualHash = await ComputeFileSHA256Async(filePath);
        return Verify(expectedHash, actualHash);
    }
}