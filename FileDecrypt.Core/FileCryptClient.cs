using FileDecrypt.Core.Decryptors;
using FileDecrypt.Core.Extractors;
using FileDecrypt.Core.Models;
using HtmlAgilityPack;
using System.Net.Http.Headers;
using static FileDecrypt.Core.Extractors.ContainerMetadataExtractor;

namespace FileDecrypt.Core;

public class FileCryptClient
{
    private readonly HttpClient _httpClient;
    private readonly ContainerMetadataExtractor _containerMetadataExtractor;
    private readonly CnlPayloadExtractor _cnlPayloadExtractor;
    private readonly CnlPayloadDecryptor _cnlPayloadDecryptor;
    private readonly DlcPayloadExtractor _dlcContainerExtractor;
    private readonly DlcPayloadDecryptor _dlcContainerDecryptor;
    private readonly LinkEntryMetadataExtractor _LinkEntryMetadataExtractor;
    private readonly RequiredHeadersExtractor _requiredHeadersExtractor;
    private readonly LinkResolver _linkResolver;

    public FileCryptClient(
        HttpClient httpClient,
        ContainerMetadataExtractor containerMetadataExtractor,
        CnlPayloadExtractor cnlPayloadExtractor,
        CnlPayloadDecryptor cnlPayloadDecryptor,
        DlcPayloadExtractor dlcContainerExtractor,
        DlcPayloadDecryptor dlcContainerDecryptor,
        LinkEntryMetadataExtractor linkEntryMetadataExtractor,
        RequiredHeadersExtractor requiredHeadersExtractor,
        LinkResolver linkResolver)
    {
        _containerMetadataExtractor = containerMetadataExtractor;
        _httpClient = httpClient;
        _cnlPayloadExtractor = cnlPayloadExtractor;
        _cnlPayloadDecryptor = cnlPayloadDecryptor;
        _dlcContainerExtractor = dlcContainerExtractor;
        _dlcContainerDecryptor = dlcContainerDecryptor;
        _LinkEntryMetadataExtractor = linkEntryMetadataExtractor;
        _requiredHeadersExtractor = requiredHeadersExtractor;
        _linkResolver = linkResolver;
    }

    public async Task<FileCryptContainer> GetContainerAsync(Uri containerUrl)
    {
        // https?://(?:www\.)?filecrypt\.cc/Container/\w+
        var response = await _httpClient.GetAsync(containerUrl);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(content);

        var containerNode = doc.DocumentNode.SelectSingleNode(@"//*[@id='page']/div[2]")!;

        var containerMetadata = _containerMetadataExtractor.Extract(containerNode);
        var linkEntriesMetadata = _LinkEntryMetadataExtractor.ParseRowEntries(
            containerNode,
            containerMetadata.CnlId is null && containerMetadata.DlcId is null // unresolved links only
        );

        IReadOnlyList<string> links = containerMetadata switch
        {
            { CnlId: not null } => _cnlPayloadExtractor
                .Extract(containerNode)
                .Pipe(payload => _cnlPayloadDecryptor.Decrypt(payload)),

            { DlcId: not null } => await _dlcContainerExtractor
                .ExtractAsync(containerNode)
                .PipeAsync(payload => _dlcContainerDecryptor.DecryptAsync(payload)),

            _ => await ResolveUnresolvedLinks(linkEntriesMetadata, response.Headers)
        };

        return BuildContainer(containerUrl, containerMetadata, links, linkEntriesMetadata);
    }

    private async Task<IReadOnlyList<string>> ResolveUnresolvedLinks(
    IReadOnlyList<LinkEntryMetadata> linkEntriesMetadata,
    HttpResponseHeaders headers)
    {
        var requiredHeaders = _requiredHeadersExtractor.GetFileCryptHeader(headers);

        var tasks = linkEntriesMetadata.Select(async entry =>
            await _linkResolver.ResolveLinkAsync(entry.UnresolvedUrl, requiredHeaders)
        );

        return await Task.WhenAll(tasks);
    }

    private FileCryptContainer BuildContainer(
        Uri containerUrl,
        FileCryptContainerMetadata fileCryptContainerMetadata,
        IReadOnlyList<string>? links = null,
        IReadOnlyList<LinkEntryMetadata>? linkEntriesMetadata = null)
    {
        var linkEntries = (linkEntriesMetadata, links) switch
        {
            (null, not null) => links.Select(link => new LinkEntry(link, null)).ToList(),
            (not null, not null) => links.Zip(linkEntriesMetadata, (link, metadata) => new LinkEntry(link, metadata.ToLinkMetadata())).ToList(),
            (not null, null) => throw new ArgumentException("Links must be provided if linkEntriesMetadata is provided."),
            (null, null) => throw new ArgumentException("Either links or linkEntriesMetadata must be provided.")
        };

        return new FileCryptContainer(
            containerUrl,
            fileCryptContainerMetadata.Title,
            fileCryptContainerMetadata.Status,
            fileCryptContainerMetadata.LastChecked,
            fileCryptContainerMetadata.CnlId,
            fileCryptContainerMetadata.DlcId,
            linkEntries);
    }
}

internal static class FunctionalExtensions
{
    public static TResult Pipe<TInput, TResult>(this TInput input, Func<TInput, TResult> func) => func(input);
    public static async Task<TResult> PipeAsync<TInput, TResult>(this Task<TInput> task, Func<TInput, Task<TResult>> func)
        => await func(await task);
}