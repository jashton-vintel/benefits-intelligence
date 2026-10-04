using BenefitsIntelligence.Application.Messaging;
using BenefitsIntelligence.Domain.Policies;
using BenefitsIntelligence.Domain.Processing;

namespace BenefitsIntelligence.Application.Processing;

public sealed class ProcessingResultHandler(IProcessingJobRepository jobs, TimeProvider timeProvider)
{
    public const int MaxFailureMessageLength = 2000;

    public async Task<ProcessingResultOutcome> HandleCompletedAsync(ProcessPolicyCompleted message, CancellationToken cancellationToken)
    {
        ProcessingJob? job = await FindJobAsync(message.CorrelationId, message.PolicyId, cancellationToken);
        if (job is null)
        {
            return ProcessingResultOutcome.NoMatchingJob;
        }

        // Only the delivery that completes the job applies its extraction, so a redelivered
        // completion can never overwrite details recorded since.
        if (!job.MarkCompleted(timeProvider.GetUtcNow()))
        {
            return ProcessingResultOutcome.AlreadyRecorded;
        }

        BenefitPolicy policy = await jobs.FindPolicyAsync(job.PolicyId, cancellationToken)
            ?? throw new InvalidOperationException($"Policy {job.PolicyId} for processing job {job.Id} does not exist.");

        PolicyExtraction extraction = message.Extraction;
        policy.RecordExtraction(extraction.Provider, extraction.SchemeName, extraction.AnnualExcess);

        await jobs.SaveChangesAsync(cancellationToken);
        return ProcessingResultOutcome.Recorded;
    }

    public async Task<ProcessingResultOutcome> HandleFailedAsync(ProcessPolicyFailed message, CancellationToken cancellationToken)
    {
        ProcessingJob? job = await FindJobAsync(message.CorrelationId, message.PolicyId, cancellationToken);
        if (job is null)
        {
            return ProcessingResultOutcome.NoMatchingJob;
        }

        // The reason comes from another service; keep it within what the job can store.
        string reason = message.ErrorMessage.Length <= MaxFailureMessageLength
            ? message.ErrorMessage
            : message.ErrorMessage[..MaxFailureMessageLength];

        if (!job.MarkFailed(message.ErrorCode, reason, timeProvider.GetUtcNow()))
        {
            return ProcessingResultOutcome.AlreadyRecorded;
        }

        await jobs.SaveChangesAsync(cancellationToken);
        return ProcessingResultOutcome.Recorded;
    }

    private async Task<ProcessingJob?> FindJobAsync(Guid correlationId, Guid policyId, CancellationToken cancellationToken)
    {
        ProcessingJob? job = await jobs.FindByCorrelationIdAsync(correlationId, cancellationToken);
        return job?.PolicyId == policyId ? job : null;
    }
}
