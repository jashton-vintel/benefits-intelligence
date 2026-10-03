using BenefitsIntelligence.Application.Processing;
using BenefitsIntelligence.Domain.Processing;

namespace BenefitsIntelligence.Application.Tests.Fakes;

internal sealed class FakeProcessingJobRepository : IProcessingJobRepository
{
    private readonly List<ProcessingJob> _jobs = [];

    public int SaveCount { get; private set; }

    public Exception? FailOnSave { get; set; }

    public void Add(ProcessingJob job) => _jobs.Add(job);

    public Task<ProcessingJob?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken) =>
        Task.FromResult(_jobs.SingleOrDefault(j => j.CorrelationId == correlationId));

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return FailOnSave is null ? Task.CompletedTask : Task.FromException(FailOnSave);
    }
}
