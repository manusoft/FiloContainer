using System.Security.Cryptography;

namespace ManuHub.Filo.Utils;

public static class FiloEncryption
{
    public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();

        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();

        return encryptor.TransformFinalBlock(data, 0, data.Length);
    }

    public static byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();

        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();

        return decryptor.TransformFinalBlock(data, 0, data.Length);
    }
}