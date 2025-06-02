# FileCrypt Container Extractor & Link Resolver

A robust C# library and toolset for extracting and resolving file container metadata and download links from [FileCrypt.co](https://filecrypt.co).

## Overview

This project enables you to programmatically fetch, parse, and fully resolve download links from FileCrypt container pages. It extracts detailed metadata such as titles, status, last checked date, dlc url, cnl url and a list of individual file entries with resolved URLs.

Built with modular components for:
- HTTP communication using `HttpClient` with custom headers and handlers
- HTML parsing via [HtmlAgilityPack](https://html-agility-pack.net/)
- Asynchronous link resolution for efficient processing

## Features

- Fetch container page HTML and parse general metadata  
- Extract all link entries—including file names, sizes, status, and download URLs—using the fastest available method: CNL, then DLC, and finally manual link resolution as a fallback.

## Getting Started

### Prerequisites

- .NET 8 SDK or later  
- NuGet package: `HtmlAgilityPack`  

### Usage Example

```csharp
var containerUrl = new Uri("https://filecrypt.co/Container/B7582E4F52.html");
var container = await filecryptClient.GetContainerAsync(containerUrl);

Console.WriteLine(container.ToString());
```
