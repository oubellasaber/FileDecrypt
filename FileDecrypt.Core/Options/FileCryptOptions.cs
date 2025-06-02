namespace FileDecrypt.Core.Options;

public class FileCryptOptions
{
    public string BaseUrl { get; set; }
    public string LinkEndpoint { get; set; }

    public static FileCryptOptions Default { get; } = new FileCryptOptions();

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