using FileDecrypt.Core.Decryptors;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace FileDecrypt.Core.Extractors;

public class CnlPayloadExtractor
{
    public CnlPayload Extract(HtmlNode node)
    {
        ArgumentNullException.ThrowIfNull(node, nameof(node));

        var cnlForm = ExtractCnlFormHtmlNode(node)
            ?? throw new InvalidOperationException("CNL form not found in HTML node.");

        var onsubmitHandler = cnlForm.GetAttributeValue("onsubmit", string.Empty);
        if (string.IsNullOrWhiteSpace(onsubmitHandler))
            throw new InvalidOperationException("No 'onsubmit' attribute found on the CNL form.");

        var arguments = ExtractArgumentsFromOnsubmit(onsubmitHandler);

        return new CnlPayload(arguments[2], arguments[1]);
    }

    public (bool Success, CnlPayload? Payload) TryExtract(HtmlNode node)
    {
        try
        {
            var payload = Extract(node);
            return (true, payload);
        }
        catch
        {
            return default;
        }
    }

    private static HtmlNode? ExtractCnlFormHtmlNode(HtmlNode node)
    {
        return node.SelectSingleNode(@"//form[contains(@onsubmit, 'CNLPOP')]");
    }

    private static string[] ExtractArgumentsFromOnsubmit(string onSubmitHandler)
    {
        var regex = new Regex(@"'(.*?)'");
        var matches = regex.Matches(onSubmitHandler);

        if (matches.Count < 2)
            throw new FormatException("Expected at least 2 arguments in 'onsubmit' handler, but found fewer.");

        var arguments = new string[matches.Count];
        for (int i = 0; i < matches.Count; i++)
        {
            arguments[i] = matches[i].Groups[1].Value;
        }

        return arguments;
    }
}
