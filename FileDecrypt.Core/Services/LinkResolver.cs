using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace FileDecrypt.Core.Services;

public class LinkResolver
{
    private readonly HttpClient _httpClient;
    private readonly FileCryptOptions _options;

    public LinkResolver(
        HttpClient httpClient,
        IOptions<FileCryptOptions> fileCryptOptions)
    {
        _httpClient = httpClient;
        _options = fileCryptOptions.Value ?? new FileCryptOptions();
    }

    public async Task<string> ResolveLinkAsync(string url, FileCryptHeader requiredHeaders)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(requiredHeaders.PhpSessionCookie.HeaderName, requiredHeaders.PhpSessionCookie.Value);

        // ssl could be established
        var response = await _httpClient.SendAsync(request);

        string content = await response.Content.ReadAsStringAsync();

        Regex regex = new Regex(@"href='(?<redirect>[^']*)'");

        Match match = regex.Match(content);

        string resolvedUrl = match.Groups["redirect"].Value;

        request = new HttpRequestMessage(HttpMethod.Get, resolvedUrl);
        request.Headers.Add(requiredHeaders.PhpSessionCookie.HeaderName, requiredHeaders.PhpSessionCookie.Value);
        response = await _httpClient.SendAsync(request);

        return response.Headers.Location.ToString();
    }
}