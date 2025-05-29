using FileDecrypt.Core.Entites.RowEntry.Enums;
using FileDecrypt.Core.Entites.RowEntry.ValueObjects;

namespace FileDecrypt.Core.Entites.RowEntry;

public record LinkEntry
{
    public string? FileName { get; private set; }
    public FileSize? FileSize { get; private set; }
    public string? Url { get; private set; }
    public Status Status { get; private set; }

    public LinkEntry(
        string? fileName, 
        FileSize? fileSize,
        string url,
        Status status)
    {
        if (fileName == "n/a")
        {
            fileName = null;
        }

        FileName = fileName;
        FileSize = fileSize;
        Url = url;
        Status = status;
    }
}
