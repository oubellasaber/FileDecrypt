using FileDecrypt.Core.Models;
using HtmlAgilityPack;

namespace FileDecrypt.Core.Extractors;

public class ContainerMetadataExtractor
{
    public FileCryptContainerMetadata Extract(HtmlNode node)
    {
        var title = node
            .SelectSingleNode("./div/div/h2")
            ?.InnerText ?? string.Empty;

        var status = node
            .SelectSingleNode("./div/div")
            ?.GetAttributeValue("class", string.Empty)
            .Split(" ")
            .LastOrDefault();

        var lastChecked = node
            .SelectSingleNode("./div/div/small/strong")
            ?.InnerText
            .Trim();

        var statusEnum = status switch
        {
            "mostonline" => ContainerStatus.Partial,
            "online" => ContainerStatus.Online,
            "offline" => ContainerStatus.Offline,
            _ => ContainerStatus.Unknown
        };

        string format = "dd.MM.yyyy - HH:mm";
        DateTime? parsedDateTime = DateTime.TryParseExact(
            lastChecked,
            format,
            null,
            System.Globalization.DateTimeStyles.None,
            out var lastCheckedDateTime)
            ? lastCheckedDateTime
            : null;

        // Extract the cnl, dlc files download links
        TryExtractDlcFileIdFromNode(node, out var dlcFileId);
        TryExtractCnlFileIdFromNode(node, out var cnlFileId);

        var container = new FileCryptContainerMetadata(title, statusEnum, lastCheckedDateTime, cnlFileId, dlcFileId);

        return container;
    }

    private static bool TryExtractDlcFileIdFromNode(HtmlNode node, out string? dlcFileId)
    {
        try
        {
            dlcFileId = DlcPayloadExtractor.ExtractFileIdFromNode(node);
            return true;
        }
        catch
        {
            dlcFileId = null;
            return false;
        }
    }

    private static bool TryExtractCnlFileIdFromNode(HtmlNode node, out string? cnlFileId)
    {
        var cnlHiddenInput = node.SelectSingleNode("//input[contains(@name, 'hidden_cnl_id')][@value]");

        if (cnlHiddenInput is null)
        {
            cnlFileId = null;
            return false;
        }

        cnlFileId = cnlHiddenInput.GetAttributeValue("value", "");

        return true;
    }

    public record FileCryptContainerMetadata(
        string Title,
        ContainerStatus Status,
        DateTime? LastChecked,
        string? CnlId,
        string? DlcId);
}
