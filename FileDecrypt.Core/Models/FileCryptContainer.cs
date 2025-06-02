using System.Text;

namespace FileDecrypt.Core.Models;

public class FileCryptContainer
{
    private readonly List<LinkEntry> _entries;

    public Uri Url { get; private set; }
    public string Title { get; private set; }
    public ContainerStatus Status { get; private set; }
    public DateTime? LastChecked { get; private set; }
    public CnlDownloadLink? CnlDownloadMetdata { get; private set; }
    public DlcDownloadLink? DlcDownloadMetdata { get; private set; }

    /// <summary>
    /// Returns the estimated total size. If a file's size is missing, it uses the size of other entries with the same filename.
    /// </summary>
    public double EstimatedTotalSize
    {
        get
        {
            // In case the container has only cnl payload and no rows with links metadata we cannot determine the total size
            if (_entries.Any(l => l.LinkMetadata is null))
            {
                return 0;
            }

            // First pass: build a dictionary of known sizes by file name
            var knownSizes = new Dictionary<string, double>();
            foreach (var entry in _entries)
            {
                if (entry.LinkMetadata!.FileName is null)
                {
                    continue;
                }

                if (entry.LinkMetadata!.FileSize?.Size > 0 &&
                    !knownSizes.ContainsKey(entry.LinkMetadata!.FileName))
                {
                    knownSizes[entry.LinkMetadata.FileName] = entry.LinkMetadata.FileSize.Size;
                }
            }

            // Second pass: sum sizes, using knownSizes as fallback for missing sizes
            double total = 0;
            foreach (var entry in _entries)
            {
                if (entry.LinkMetadata!.FileName is null)
                {
                    continue;
                }

                if (entry.LinkMetadata!.FileSize?.Size > 0)
                {
                    total += entry.LinkMetadata.FileSize.Size;
                }
                else if (knownSizes.TryGetValue(entry.LinkMetadata.FileName, out var fallbackSize))
                {
                    total += fallbackSize;
                }
            }

            return total;
        }
    }

    /// <summary>
    /// Gets the total size of all link entries, summing only the explicitly available sizes.
    /// This does not account for missing sizes or estimate based on duplicates.
    /// </summary>
    /// <remarks>
    /// Consider using <see cref="EstimatedTotalSize"/> for a more accurate approximation
    /// when some entries are missing file size information.
    /// </remarks>
    public double TotalSize
        => _entries.Sum(entry => entry.LinkMetadata?.FileSize?.Size ?? 0);


    public IReadOnlyList<LinkEntry> LinkEntries
        => _entries.AsReadOnly();

    internal FileCryptContainer(
        Uri url,
        string title,
        ContainerStatus status,
        DateTime? lastChecked,
        string? cnlId,
        string? dlcId,
        List<LinkEntry> linkEntries)
    {
        Url = url;
        Title = title;
        Status = status;
        LastChecked = lastChecked;
        CnlDownloadMetdata = cnlId is not null ? new CnlDownloadLink(cnlId) : null;
        DlcDownloadMetdata = dlcId is not null ? new DlcDownloadLink(dlcId) : null;
        _entries = linkEntries ?? new List<LinkEntry>();
    }

    internal FileCryptContainer(
        Uri url,
        string title,
        ContainerStatus status,
        DateTime? lastChecked,
        string? cnlId,
        string? dlcId) :
        this
        (
            url,
            title,
            status,
            lastChecked,
            cnlId,
            dlcId,
            new List<LinkEntry>())
    {
    }

    public void AddLinkEntry(LinkEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (_entries.Contains(entry))
        {
            throw new InvalidOperationException("Entry already exists in the container.");
        }

        _entries.Add(entry);
    }

    public void AddRangeLinkEntries(IEnumerable<LinkEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        foreach (var entry in entries)
        {
            AddLinkEntry(entry);
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        // Header with basic container info
        sb.AppendLine($"FileCrypt Container: {Title}");
        sb.AppendLine($"URL: {Url}");
        sb.AppendLine($"Status: {Status}");

        if (LastChecked.HasValue)
        {
            sb.AppendLine($"Last Checked: {LastChecked:yyyy-MM-dd HH:mm:ss}");
        }

        // Download links section
        if (CnlDownloadMetdata is not null || DlcDownloadMetdata is not null)
        {
            sb.AppendLine();
            sb.AppendLine("Download Links:");

            if (CnlDownloadMetdata is not null)
            {
                sb.AppendLine($"  CNL: {CnlDownloadMetdata.Value}");
            }

            if (DlcDownloadMetdata is not null)
            {
                sb.AppendLine($"  DLC: {DlcDownloadMetdata.Value}");
            }
        }

        // Size information
        sb.AppendLine();
        sb.AppendLine($"Files: {_entries.Count}");

        if (TotalSize > 0 || EstimatedTotalSize > 0)
        {
            if (Math.Abs(TotalSize - EstimatedTotalSize) < 0.001) // They're essentially equal
            {
                sb.AppendLine($"Total Size: {TotalSize}  GB");
            }
            else
            {
                sb.AppendLine($"Total Size: {TotalSize} GB");
                sb.AppendLine($"Estimated Size: {EstimatedTotalSize:F2}  GB");
            }
        }

        // File entries section
        if (_entries.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("File Entries:");

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                sb.AppendLine($"  [{i + 1:D3}] {FormatLinkEntry(entry)}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatLinkEntry(LinkEntry entry)
    {
        var parts = new List<string>();

        // Add filename if available
        if (!string.IsNullOrEmpty(entry.LinkMetadata?.FileName))
        {
            parts.Add(entry.LinkMetadata.FileName);
        }

        // Add file size if available
        if (entry.LinkMetadata?.FileSize?.Size > 0)
        {
            parts.Add($"({entry.LinkMetadata.FileSize.Size} {entry.LinkMetadata.FileSize.Unit})");
        }

        // Add URL if available (truncated if too long)
        if (!string.IsNullOrEmpty(entry.Url?.ToString()))
        {
            var url = entry.Url.ToString();

            parts.Add($"- {url}");
        }

        return parts.Count > 0 ? string.Join(" ", parts) : "No metadata available";
    }
}

public record CnlDownloadLink
{
    public string Id { get; private set; }
    public int? Season { get; private set; }
    public int? Episode { get; private set; }

    public Uri Value => new($"https://filecrypt.co/_CNL/{Id}.html?season={Season}&episode={Episode}");

    public CnlDownloadLink(string id, int? season = null, int? episode = null)
    {
        if (string.IsNullOrEmpty(id))
        {
            ArgumentNullException.ThrowIfNull(id);
        }

        Id = id;
        Season = season;
        Episode = episode;
    }
}

public record DlcDownloadLink
{
    public string Id { get; private set; }

    public Uri Value => new($"https://filecrypt.cc/DLC/{Id}.dlc");

    public DlcDownloadLink(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            ArgumentNullException.ThrowIfNull(id);
        }

        Id = id;
    }
}

public enum ContainerStatus
{
    Online,
    Offline,
    Partial,
    Unknown
}