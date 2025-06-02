using FileDecrypt.Core.Decryptors;
using FileDecrypt.Core.Models;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace FileDecrypt.Core.Extractors;

public class DlcPayloadExtractor
{
    private readonly HttpClient _httpClient;

    public DlcPayloadExtractor(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DlcPayload> ExtractAsync(HtmlNode node)
    {
        ArgumentNullException.ThrowIfNull(node, nameof(node));

        var fileId = ExtractFileIdFromNode(node);
        var dlcDownloadLink = new DlcDownloadLink(fileId);

        var response = await _httpClient.GetAsync(dlcDownloadLink.Value);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Failed to retrieve DLC payload. Status: {response.StatusCode}");

        var content = await response.Content.ReadAsStringAsync();

        return new DlcPayload(content);
    }

    public static string ExtractFileIdFromNode(HtmlNode node)
    {
        var dlcDownloadBtn = ExtractDlcDownloadBtnHtmlNode(node)
            ?? throw new InvalidOperationException("DLC download button not found in HTML node.");

        var onClickHandler = dlcDownloadBtn.GetAttributeValue("onclick", string.Empty);
        if (string.IsNullOrWhiteSpace(onClickHandler))
            throw new InvalidOperationException("No 'onclick' attribute found on the DLC button.");

        var regex = new Regex(@"DownloadDLC\('([^']*)'\)");

        var match = regex.Match(onClickHandler);
        if (!match.Success)
            throw new FormatException("Unable to extract file ID from 'onclick' handler.");

        return match.Groups[1].Value;
    }

    public async Task<(bool Success, DlcPayload? Payload)> TryExtract(HtmlNode node)
    {
        try
        {
            var payload = await ExtractAsync(node);
            return (true, payload);
        }
        catch
        {
            return default;
        }
    }

    private static HtmlNode? ExtractDlcDownloadBtnHtmlNode(HtmlNode node)
    {
        return node.SelectSingleNode(@"//button[contains(@onclick, 'DownloadDLC')]");
    }
}
