using System.Security.Cryptography;
using System.Text;

namespace FileDecrypt.Core.Entites.EncryptedContainers;

public record CnlPayload
{
    private readonly string KeyHex;
    private readonly string EncryptedBase64;
    private readonly string? Password = null;

    public CnlPayload(string keyHex, string encryptedBase64, string? password = null)
    {
        ArgumentNullException.ThrowIfNull(keyHex);
        ArgumentNullException.ThrowIfNull(encryptedBase64);

        KeyHex = keyHex;
        EncryptedBase64 = encryptedBase64;
        Password = password;
    }

    public IReadOnlyList<string> Decrypt()
    {
        // Convert hex key to byte array
        byte[] key = ConvertHexStringToByteArray(KeyHex);

        // Convert base64 to byte array
        byte[] cipherText = Convert.FromBase64String(EncryptedBase64);

        // Decrypt using AES CBC with no padding
        string plainText = Decrypt(cipherText, key);

        // Clean up and extract links
        string cleaned = ExtractLinks(plainText);

        var links = cleaned.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

        return links;
    }

    static byte[] ConvertHexStringToByteArray(string hex)
    {
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }

    static string Decrypt(byte[] cipherText, byte[] key)
    {
        using Aes aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        aes.Key = key;
        aes.IV = key;

        using ICryptoTransform decryptor = aes.CreateDecryptor();
        byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);

        // Let's UTF-8 first, fallback to Latin1
        try
        {
            return Encoding.UTF8.GetString(decryptedBytes).Trim('\0');
        }
        catch
        {
            return Encoding.GetEncoding("ISO-8859-1").GetString(decryptedBytes).Trim('\0');
        }
    }

    static string ExtractLinks(string raw)
    {
        // Find first http and normalize whitespace
        int start = raw.IndexOf("http", StringComparison.OrdinalIgnoreCase);
        if (start >= 0)
        {
            raw = raw.Substring(start);
        }
        return raw.Trim();
    }
}
