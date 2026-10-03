using BenefitsIntelligence.Application.Policies;
using BenefitsIntelligence.Domain.Policies;
using BenefitsIntelligence.Domain.Processing;

namespace BenefitsIntelligence.Application.Tests.Fakes;

internal sealed class FakePolicyRepository(CallLog log) : IPolicyRepository
{
    private int _saveCount;

    public PolicyDocument? Document { get; private set; }

    public BenefitPolicy? Policy { get; private set; }

    public ProcessingJob? Job { get; private set; }

    /// <summary>
    /// Exceptions to throw, keyed by the 1-based number of the save call.
    /// </summary>
    public Dictionary<int, Exception> FailOnSave { get; } = [];

    public void Add(PolicyDocument document, BenefitPolicy policy, ProcessingJob job)
    {
        Document = document;
        Policy = policy;
        Job = job;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        _saveCount++;
        log.Record($"repository.save({Job?.Status})");

        return FailOnSave.TryGetValue(_saveCount, out Exception? exception)
            ? Task.FromException(exception)
            : Task.CompletedTask;
    }
}
