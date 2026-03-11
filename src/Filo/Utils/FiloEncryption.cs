using System.Security.Cryptography;
using System.Text;

namespace ManuHub.Filo.Utils;

public static class FiloEncryption
{
    private static readonly byte[] DefaultKey = Encoding.UTF8.GetBytes("12345678901234561234567890123456");

    public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(data, 0, data.Length);
    }

    public static byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(data, 0, data.Length);
    }
}