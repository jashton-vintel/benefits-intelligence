namespace BenefitsIntelligence.Domain.Policies;

public sealed class PolicyDocument
{
    private readonly List<DocumentPage> _pages = [];

    public PolicyDocument(
        Guid id,
        Guid organisationId,
        string fileName,
        string storagePath,
        string contentType,
        long sizeBytes,
        DateTimeOffset uploadedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizeBytes);

        Id = id;
        OrganisationId = organisationId;
        FileName = fileName;
        StoragePath = storagePath;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        UploadedAt = uploadedAt;
    }

    public Guid Id { get; }

    public Guid OrganisationId { get; }

    public string FileName { get; }

    /// <summary>
    /// Location relative to the document store root, so the same value resolves on any host.
    /// </summary>
    public string StoragePath { get; }

    public string ContentType { get; }

    public long SizeBytes { get; }

    public DateTimeOffset UploadedAt { get; }

    public IReadOnlyCollection<DocumentPage> Pages => _pages.AsReadOnly();

    /// <summary>Replaces the stored text with the pages read from the document.</summary>
    public void RecordPages(IEnumerable<DocumentPage> pages)
    {
        ArgumentNullException.ThrowIfNull(pages);

        List<DocumentPage> ordered = pages.OrderBy(p => p.PageNumber).ToList();
        if (ordered.Count == 0 || ordered.Where((page, index) => page.PageNumber != index + 1).Any())
        {
            throw new ArgumentException("Pages must be numbered consecutively from 1.", nameof(pages));
        }

        _pages.Clear();
        _pages.AddRange(ordered);
    }
}
