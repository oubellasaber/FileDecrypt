using FileDecrypt.Core.Models;
using FileDecrypt.Core.Options;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FileDecrypt.Core.Extractors;

public class LinkEntryMetadataExtractor
{
    private readonly FileCryptOptions _options;

    public LinkEntryMetadataExtractor(IOptions<FileCryptOptions> fileCryptOptions)
    {
        _options = fileCryptOptions.Value ?? FileCryptOptions.Default;
    }

    public LinkEntryMetadata ParseRowEntry(HtmlNode node, bool includeUnresolvedLink = false)
    {
        ArgumentNullException.ThrowIfNull(node);

        string? id = null;

        if (includeUnresolvedLink)
        {
            id = GetFirstDataAttribute(node)?.Value;
        }

        var unresolvedUrl = id is null ? null : GetLinkUrl(id);
        var fileName = node.SelectSingleNode(".//td/@title")?.GetAttributeValue("title", "");
        var statusString = node
            .SelectSingleNode(".//td[@class = 'status']/i")
            ?.GetAttributeValue("class", "")
            .Split(" ")
            .FirstOrDefault();
        var rawFileSize = node.SelectSingleNode("./td[3]")?.InnerText ?? string.Empty;

        var status = statusString switch
        {
            "online" => LinkStatus.Online,
            "offline" => LinkStatus.Offline,
            _ => LinkStatus.Unknown
        };


        string pattern = @"^(?<size>\d+(\.\d+)?)\s?(?<unit>GB|MB)$";

        FileSize? fileSize = null;

        Match match = Regex.Match(rawFileSize, pattern);

        if (match.Success)
        {
            double size = double.Parse(match.Groups["size"].Value);
            var unit = Enum.Parse<DataMeasurement>(match.Groups["unit"].Value);

            fileSize = new FileSize(size, unit);
        }

        var parsedRow = new LinkEntryMetadata(fileName, fileSize, status, unresolvedUrl);

        return parsedRow;
    }

    public IReadOnlyList<LinkEntryMetadata>? ParseRowEntries(HtmlNode node, bool includeUnresolvedLinks = false)
    {
        var entries = new List<LinkEntryMetadata>();
        var hasTableWithTr = node.SelectNodes("//table[.//tr]")?.Any() == true;

        var nodes = node.SelectNodes("//table//tr");

        if (nodes == null || nodes.Count == 0)
        {
            return null;
        }

        foreach (var rowNode in nodes)
        {
            var entry = ParseRowEntry(rowNode, includeUnresolvedLinks);
            entries.Add(entry);
        }

        return entries;
    }

    private static HtmlAttribute? GetFirstDataAttribute(HtmlNode node)
    {
        return node.SelectSingleNode(".//button")?.Attributes
                   .FirstOrDefault(attr => attr.Name.StartsWith("data-"));
    }

    private string GetLinkUrl(string linkId)
    {
        return $"{_options.BaseUrl}/{_options.LinkEndpoint}/{linkId}.html";
    }
}

public record LinkEntryMetadata
{
    public string? FileName { get; private set; }
    public FileSize? FileSize { get; private set; }
    public LinkStatus Status { get; private set; }
    public string? UnresolvedUrl { get; private set; }

    public LinkEntryMetadata(
        string? fileName,
        FileSize? fileSize,
        LinkStatus status,
        string unresolvedUrl)
    {
        FileName = fileName;
        FileSize = fileSize;
        Status = status;
        UnresolvedUrl = unresolvedUrl;
    }

    public LinkMetadata ToLinkMetadata()
    {
        return new LinkMetadata(FileName, FileSize, Status);
    }
}
