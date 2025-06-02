namespace FileDecrypt.Core.Models;

public record LinkEntry
{
    public LinkMetadata? LinkMetadata { get; private set; }
    public string Url { get; private set; }

    public LinkEntry(
        string url,
        LinkMetadata? linkMetadata)
    {
        LinkMetadata = linkMetadata;
        Url = url;
    }

    public LinkEntry(
        string? fileName,
        FileSize? fileSize,
        LinkStatus status,
        string url)
    {
        LinkMetadata = new LinkMetadata(fileName, fileSize, status);
        Url = url;
    }
}

public class FileSize
{
    public double Size { get; }
    public DataMeasurement Unit { get; }

    public FileSize(double size, DataMeasurement unit)
    {
        if (size < 0)
            throw new ArgumentException("File size cannot be negative.", nameof(size));

        Size = size;
        Unit = unit;
    }

    public override string ToString() => $"{Size:F2} {Unit}";
}

public record LinkMetadata
{
    public string? FileName { get; private set; }
    public FileSize? FileSize { get; private set; }
    public LinkStatus Status { get; private set; }

    public LinkMetadata(
        string? fileName,
        FileSize? fileSize,
        LinkStatus status)
    {
        if (fileName == "n/a")
        {
            fileName = null;
        }

        FileName = fileName;
        FileSize = fileSize;
        Status = status;
    }
}

public enum DataMeasurement
{
    MB = 1,
    GB = 2
}

public enum LinkStatus
{
    Online = 1,
    Offline = 2,
    Unknown = 3
}
