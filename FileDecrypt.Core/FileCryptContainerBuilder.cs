using FileDecrypt.Core.Entites.Container;
using FileDecrypt.Core.Entites.RowEntry;
using FileDecrypt.Core.Services;
using HtmlAgilityPack;

namespace FileDecrypt.Core;

public class FileCryptContainerBuilder
{
    private readonly HttpClient _httpClient;
    private readonly RequiredHeadersExtractor _requiredHeadersExtractor;
    private readonly ContainerMetadataExtractor _containerMetadataExtractor;
    private readonly LinkEntryExtractor _linkEntryExtractor;
    private readonly LinkResolver _linkResolver;

    public FileCryptContainerBuilder(
        HttpClient httpClient,
        RequiredHeadersExtractor requiredHeadersExtractor,
        ContainerMetadataExtractor containerMetadataExtractor,
        LinkEntryExtractor fileCryptLinkEntryExtractor,
        LinkResolver linkResolver)
    {
        _httpClient = httpClient;
        _requiredHeadersExtractor = requiredHeadersExtractor;
        _containerMetadataExtractor = containerMetadataExtractor;
        _linkEntryExtractor = fileCryptLinkEntryExtractor;
        _linkResolver = linkResolver;
    }

    public async Task<FileCryptContainer> BuildContainerAsync(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url, nameof(url));

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        var document = new HtmlDocument();
        document.LoadHtml(content);

        var containerNode = document
            .DocumentNode
            .SelectSingleNode(@"//*[@id='page']/div[2]");

        ArgumentNullException.ThrowIfNull(containerNode, "Container node not found in the document.");

        var requiredHeaders = _requiredHeadersExtractor.GetFileCryptHeader(response.Headers);

        var containerMetadata = _containerMetadataExtractor
            .ParseContainer(containerNode);

        var rawLinkEntries = _linkEntryExtractor.ParseRowEntries(containerNode);

        var linkResolutionTasks = rawLinkEntries
            .Select(async entry =>
            {
                var resolvedUrl = await _linkResolver.ResolveLinkAsync(entry.Url, requiredHeaders);
                return new LinkEntry(entry.FileName, entry.FileSize, resolvedUrl, entry.Status);
            });

        var resolvedLinkEntries = await Task.WhenAll(linkResolutionTasks);

        var container = new FileCryptContainer(
            url,
            containerMetadata.Title,
            containerMetadata.Status,
            containerMetadata.LastChecked,
            resolvedLinkEntries.ToList());

        return container;
    }
}
