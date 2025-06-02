using System.Net.Http.Headers;

namespace FileDecrypt.Core.Extractors;

public record HttpHeader(string HeaderName, string Value)
{
    public string Value { set; get; } = Value;
}

public record CookieHeader(string Value) : HttpHeader("Cookie", Value);

public class FileCryptHeader
{
    public FileCryptHeader(string phpSessionCookie)
    {
        PhpSessionCookie = new CookieHeader($"PHPSESSID={phpSessionCookie}");
    }

    public HttpHeader PhpSessionCookie { get; private set; }
}

public class RequiredHeadersExtractor
{
    public FileCryptHeader GetFileCryptHeader(HttpResponseHeaders headers)
    {
        string requiredHeaders = headers.GetValues("Set-Cookie").First();

        var headersSeperated = requiredHeaders.Split(';');
        var phpSessid = headersSeperated[0].Split('=')[1];
        var header = new FileCryptHeader(phpSessid);

        return header;
    }
}
