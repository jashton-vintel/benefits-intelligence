using BenefitsIntelligence.Application.Processing;
using BenefitsIntelligence.Domain.Policies;
using BenefitsIntelligence.Domain.Processing;

namespace BenefitsIntelligence.Application.Tests.Fakes;

internal sealed class FakeProcessingJobRepository : IProcessingJobRepository
{
    private readonly List<ProcessingJob> _jobs = [];
    private readonly List<BenefitPolicy> _policies = [];
    private readonly List<PolicyDocument> _documents = [];

    public int SaveCount { get; private set; }

    public Exception? FailOnSave { get; set; }

    public void Add(ProcessingJob job) => _jobs.Add(job);

    public void Add(BenefitPolicy policy) => _policies.Add(policy);

    public void Add(PolicyDocument document) => _documents.Add(document);

    public Task<ProcessingJob?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken) =>
        Task.FromResult(_jobs.SingleOrDefault(j => j.CorrelationId == correlationId));

    public Task<BenefitPolicy?> FindPolicyAsync(Guid policyId, CancellationToken cancellationToken) =>
        Task.FromResult(_policies.SingleOrDefault(p => p.Id == policyId));

    public Task<PolicyDocument?> FindDocumentAsync(Guid documentId, CancellationToken cancellationToken) =>
        Task.FromResult(_documents.SingleOrDefault(d => d.Id == documentId));

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return FailOnSave is null ? Task.CompletedTask : Task.FromException(FailOnSave);
    }
}
