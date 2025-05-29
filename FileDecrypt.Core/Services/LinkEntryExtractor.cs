using FileDecrypt.Core.Entites.RowEntry;
using FileDecrypt.Core.Entites.RowEntry.Enums;
using FileDecrypt.Core.Entites.RowEntry.ValueObjects;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace FileDecrypt.Core.Services;

public class LinkEntryExtractor
{
    private readonly FileCryptOptions _options;

    public LinkEntryExtractor(IOptions<FileCryptOptions> fileCryptOptions)
    {
        _options = fileCryptOptions.Value ?? new FileCryptOptions();
    }

    public RawLinkEntry ParseRowEntry(HtmlNode node)
    {
        var id = GetFirstDataAttribute(node)?.Value;

        ArgumentNullException.ThrowIfNull(id);

        var unresolvedUrl = GetLinkUrl(id);
        var fileName = node.SelectSingleNode(".//td/@title")?.GetAttributeValue("title", "");
        var statusString = node
            .SelectSingleNode(".//td[@class = 'status']/i")
            ?.GetAttributeValue("class", "")
            .Split(" ")
            .FirstOrDefault();
        var rawFileSize = node.SelectSingleNode("./td[3]")?.InnerText ?? string.Empty;

        var status = statusString switch
        {
            "online" => Status.Online,
            "offline" => Status.Offline,
            _ => Status.Unknown
        };


        string pattern = @"^(?<size>\d+(\.\d+)?)\s?(?<unit>GB|MB)$";

        FileSize? fileSize = null;

        Match match = Regex.Match(rawFileSize, pattern);

        if (match.Success)
        {
            double size = double.Parse(match.Groups["size"].Value);
            DataMeasurement unit = Enum.Parse<DataMeasurement>(match.Groups["unit"].Value);

            fileSize = new FileSize(size, unit);
        }

        var parsedRow = new RawLinkEntry(fileName, fileSize, unresolvedUrl, status);

        return parsedRow;
    }

    public IReadOnlyList<RawLinkEntry> ParseRowEntries(HtmlNode doc)
    {
        var entries = new List<RawLinkEntry>();
        var nodes = doc.SelectNodes("//table//tr");

        if (nodes == null || nodes.Count == 0)
        {
            return entries;
        }

        foreach (var node in nodes)
        {
            var entry = ParseRowEntry(node);
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

    public record RawLinkEntry : LinkEntry
    {
        public RawLinkEntry(
            string? fileName,
            FileSize? fileSize,
            string unresolvedUrl,
            Status status) :
            base(fileName, fileSize, unresolvedUrl, status)
        {
        }
    }
}

public class FileCryptOptions
{
    public string BaseUrl { get; set; }
    public string LinkEndpoint { get; set; }

    public FileCryptOptions()
    {
        BaseUrl = "https://filecrypt.co";
        LinkEndpoint = "Link";
    }

    public FileCryptOptions(string baseUrl, string linkEndpoint)
    {
        BaseUrl = baseUrl;
        LinkEndpoint = linkEndpoint;
    }
}