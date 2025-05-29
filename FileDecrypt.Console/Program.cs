using FileDecrypt.Core;
using FileDecrypt.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http;

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

host.Services.AddTransient<RequiredHeadersExtractor>();
host.Services.AddTransient<ContainerMetadataExtractor>();
host.Services.AddTransient<LinkEntryExtractor>();
host.Services.AddTransient<FileCryptContainerBuilder>();

// host.Services.AddLogging(config => config.AddConsole());

using var app = host.Build();

// Resolve and run
var builder = app.Services.GetRequiredService<FileCryptContainerBuilder>();
var container = await builder.BuildContainerAsync(new Uri("https://filecrypt.co/Container/B3CFD96DB8.html"));

Console.WriteLine($"Built container: {container.Title}");
