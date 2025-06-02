using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace FileDecrypt.Core.Decryptors;

public class CnlPayloadDecryptor
{
    public IReadOnlyList<string> Decrypt(CnlPayload cnlPayload)
    {
        // Convert hex key to byte array
        byte[] key = ConvertHexStringToByteArray(cnlPayload.KeyHex);

        // Convert base64 to byte array
        byte[] cipherText = Convert.FromBase64String(cnlPayload.EncryptedBase64);

        // Decrypt using AES CBC with no padding
        string plainText = Decrypt(cipherText, key);

        // Clean up and extract links
        string cleaned = ExtractLinks(plainText);

        var links = cleaned.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

        return links;
    }

    private static byte[] ConvertHexStringToByteArray(string hex)
    {
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }

    private static string Decrypt(byte[] cipherText, byte[] key)
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

    private static string ExtractLinks(string raw)
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

public record CnlPayload
{
    public string KeyHex { get; }
    public string EncryptedBase64 { get; }
    public string? Password { get; }

    public CnlPayload(string encryptedBase64, string keyHex, string? password = null)
    {
        if (string.IsNullOrWhiteSpace(keyHex) || !Regex.IsMatch(keyHex, @"^[0-9a-fA-F]+$"))
            throw new ArgumentException("Key must be a valid hexadecimal string.", nameof(keyHex));

        ArgumentNullException.ThrowIfNull(encryptedBase64);

        KeyHex = keyHex;
        EncryptedBase64 = encryptedBase64;
        Password = password;
    }
}
