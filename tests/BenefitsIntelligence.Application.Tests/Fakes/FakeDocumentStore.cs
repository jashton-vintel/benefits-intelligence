using BenefitsIntelligence.Application.Documents;

namespace BenefitsIntelligence.Application.Tests.Fakes;

internal sealed class FakeDocumentStore(CallLog log) : IDocumentStore
{
    public List<string> Deleted { get; } = [];

    public Task<string> SaveAsync(Guid organisationId, Guid documentId, Stream content, CancellationToken cancellationToken)
    {
        log.Record("store.save");
        return Task.FromResult($"documents/{organisationId:N}/{documentId:N}.pdf");
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken)
    {
        log.Record("store.delete");
        Deleted.Add(storagePath);
        return Task.CompletedTask;
    }
}
