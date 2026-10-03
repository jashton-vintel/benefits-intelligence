namespace BenefitsIntelligence.Application.Documents;

public interface IDocumentStore
{
    /// <summary>
    /// Stores the document and returns its storage path, relative to the store root.
    /// </summary>
    Task<string> SaveAsync(Guid organisationId, Guid documentId, Stream content, CancellationToken cancellationToken);

    Task DeleteAsync(string storagePath, CancellationToken cancellationToken);
}
