using FileDecrypt.Core.Entites.Container.Enums;
using FileDecrypt.Core.Entites.RowEntry;
using System;

namespace FileDecrypt.Core.Entites.Container;

public class FileCryptContainer
{
    private readonly List<LinkEntry> _entries;

    public Uri Url { get; private set; }
    public string Title { get; private set; }
    public Status Status { get; private set; }
    public DateTime? LastChecked { get; private set; }

    /// <summary>
    /// Returns the estimated total size. If a file's size is missing, it uses the size of other entries with the same filename.
    /// </summary>
    public double EstimatedTotalSize
    {
        get
        {
            // First pass: build a dictionary of known sizes by file name
            var knownSizes = new Dictionary<string, double>();
            foreach (var entry in _entries)
            {
                if (entry.FileSize?.Size > 0 && !knownSizes.ContainsKey(entry.FileName))
                {
                    knownSizes[entry.FileName] = entry.FileSize.Size;
                }
            }

            // Second pass: sum sizes, using knownSizes as fallback for missing sizes
            double total = 0;
            foreach (var entry in _entries)
            {
                if (entry.FileSize?.Size > 0)
                {
                    total += entry.FileSize.Size;
                }
                else if (knownSizes.TryGetValue(entry.FileName, out var fallbackSize))
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
    public double TotalSize => _entries.Sum(entry => entry.FileSize?.Size ?? 0);
    public IReadOnlyList<LinkEntry> LinkEntries 
        => _entries.AsReadOnly();

    internal FileCryptContainer(
        Uri url, 
        string title, 
        Status status, 
        DateTime? lastChecked, 
        List<LinkEntry> linkEntries)
    {
        Url = url;
        Title = title;
        Status = status;
        LastChecked = lastChecked;
        _entries = linkEntries ?? new List<LinkEntry>();
    }

    internal FileCryptContainer(
        Uri url,
        string title,
        Status status,
        DateTime? lastChecked) :
        this
        (
            url,
            title,
            status,
            lastChecked, new List<LinkEntry>())
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
}