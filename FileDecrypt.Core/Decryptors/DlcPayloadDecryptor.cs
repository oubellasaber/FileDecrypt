using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace FileDecrypt.Core.Decryptors;

public class DlcPayloadDecryptor
{
    private const string GETKEY_URL = "http://service.jdownloader.org/dlcrypt/service.php?srcType=dlc&destType=pylo&data=";
    private const string KEY_REGEX = @"<rc>([^<]+)<\/rc>";

    private const string AES_KEY = "cb99b5cbc24db398";
    private const string AES_IV = "9bc24cb995cb8db3";

    private readonly HttpClient _httpClient;

    public DlcPayloadDecryptor(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<string>> DecryptAsync(DlcPayload dlcPayload)
    {
        // Extract key and data from payload
        var sourceKey = dlcPayload.Content[^88..]; // Last 88 characters
        var sourceData = dlcPayload.Content[..^88]; // Everything except last 88 characters

        // Get real key from JDownloader service
        var realKey = Convert.FromBase64String(await GetKeyAsync(sourceKey));

        // Decrypt IV using hardcoded AES key/iv
        var realIV = AesDecrypt(realKey, Encoding.ASCII.GetBytes(AES_KEY), Encoding.ASCII.GetBytes(AES_IV));

        // Decrypt the main content (double base64 + AES)
        var encryptedData = Convert.FromBase64String(sourceData);
        var decryptedAes = AesDecrypt(encryptedData, realIV, realIV);
        var decryptedB64 = Encoding.ASCII.GetString(decryptedAes).TrimEnd('\0');
        var xmlContent = Encoding.ASCII.GetString(Convert.FromBase64String(decryptedB64));

        // Parse XML and extract URLs
        return ExtractUrlsFromXml(xmlContent);
    }

    private async Task<string> GetKeyAsync(string dlcKey)
    {
        try
        {
            var response = await _httpClient.GetStringAsync(GETKEY_URL + dlcKey);

            if (string.IsNullOrEmpty(response))
                throw new InvalidOperationException("Got empty response from key server");

            var match = Regex.Match(response, KEY_REGEX);
            if (!match.Success)
                throw new InvalidOperationException($"Invalid key response: {response}");

            return match.Groups[1].Value;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Unable to obtain key from API. Please check your internet connection", ex);
        }
    }

    private static byte[] AesDecrypt(byte[] content, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.BlockSize = 128;
        aes.KeySize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.Zeros;
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(content, 0, content.Length);
    }

    private static IReadOnlyList<string> ExtractUrlsFromXml(string xmlContent)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xmlContent);

        var urls = new List<string>();
        var fileNodes = doc.SelectNodes("//file/url");

        if (fileNodes != null)
        {
            foreach (XmlNode urlNode in fileNodes)
            {
                if (!string.IsNullOrEmpty(urlNode?.InnerText))
                {
                    // URL is base64 encoded in the XML
                    var decodedUrl = Encoding.ASCII.GetString(Convert.FromBase64String(urlNode.InnerText));
                    urls.Add(decodedUrl);
                }
            }
        }

        return urls.AsReadOnly();
    }
}

public record DlcPayload
{
    private const int DLC_KEYSIZE = 88;

    public string Content { get; }

    public DlcPayload(string content)
    {
        if (string.IsNullOrEmpty(content) || content.Length <= DLC_KEYSIZE)
        {
            throw new ArgumentException("Invalid DLC Content");
        }

        Content = content;
    }
}