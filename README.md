# FileCrypt Container Extractor & Link Resolver

A robust C# library and toolset for extracting and resolving file container metadata and download links from [FileCrypt.co](https://filecrypt.co).

## Overview

This project enables you to programmatically fetch, parse, and fully resolve download links from FileCrypt container pages. It extracts detailed metadata such as titles, status, last checked date, and a list of individual file entries with resolved URLs.

Built with modular components for:
- HTTP communication using `HttpClient` with custom headers and handlers
- HTML parsing via [HtmlAgilityPack](https://html-agility-pack.net/)
- Asynchronous link resolution for efficient processing

## Features

- Fetch container page HTML and parse general metadata  
- Extract all link entries including file names, sizes, status and download link

## Getting Started

### Prerequisites

- .NET 8 SDK or later  
- NuGet package: `HtmlAgilityPack`  

### Usage Example

```csharp
var builder = new FileCryptContainerBuilder(
    httpClient, 
    requiredHeadersExtractor, 
    containerMetadataExtractor, 
    linkEntryExtractor, 
    linkResolver);

Uri containerUrl = new Uri("https://filecrypt.co/Container/id_here_");
FileCryptContainer container = await builder.BuildContainerAsync(containerUrl);

Console.WriteLine($"Container Title: {container.Title}");
Console.WriteLine($"Total Links: {container.LinkEntries.Count}");
