using FileDecrypt.Core.Entites.Container.Enums;
using HtmlAgilityPack;

namespace FileDecrypt.Core.Services;

public class ContainerMetadataExtractor
{
    public FileCryptContainerMetadata ParseContainer(HtmlNode node)
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
            "mostonline" => Status.Partial,
            "online" => Status.Online,
            "offline" => Status.Offline,
            _ => Status.Unknown
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

        var container = new FileCryptContainerMetadata(title, statusEnum, lastCheckedDateTime);

        return container;
    }

    public record FileCryptContainerMetadata(string Title, Status Status, DateTime? LastChecked);
}
