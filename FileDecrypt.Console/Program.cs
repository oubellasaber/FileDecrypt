using FileDecrypt.Core;
using FileDecrypt.Core.Decryptors;
using FileDecrypt.Core.Extractors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateApplicationBuilder(args);

host.Services.AddHttpClient(string.Empty, client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("FileDecrypt/0.1");
});

host.Services.AddHttpClient<LinkResolver>()
    .ConfigureHttpClient(client =>
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd("FileDecrypt/0.1");
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new HttpClientHandler
        {
            AllowAutoRedirect = false
        };
    });

host.Services.AddHttpClient();
host.Services.AddTransient<ContainerMetadataExtractor>();
host.Services.AddTransient<CnlPayloadExtractor>();
host.Services.AddTransient<CnlPayloadDecryptor>();
host.Services.AddTransient<DlcPayloadExtractor>();
host.Services.AddTransient<DlcPayloadDecryptor>();
host.Services.AddTransient<LinkEntryMetadataExtractor>();
host.Services.AddTransient<RequiredHeadersExtractor>();
host.Services.AddTransient<FileCryptClient>();

// host.Services.AddLogging(config => config.AddConsole());

using var app = host.Build();

var client = app.Services.GetRequiredService<FileCryptClient>();
var container = await client.GetContainerAsync(new Uri("https://filecrypt.cc/Container/81C4E404B7.html"));

Console.WriteLine(container.ToString());