namespace BenefitsIntelligence.Domain.Policies;

public sealed class PolicyDocument
{
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
}
