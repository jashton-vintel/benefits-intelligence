using BenefitsIntelligence.Application.Messaging;
using BenefitsIntelligence.Domain.Processing;

namespace BenefitsIntelligence.Application.Processing;

public sealed class ProcessingResultHandler(IProcessingJobRepository jobs, TimeProvider timeProvider)
{
    public const int MaxFailureMessageLength = 2000;

    public Task<ProcessingResultOutcome> HandleCompletedAsync(ProcessPolicyCompleted message, CancellationToken cancellationToken) =>
        RecordAsync(message.CorrelationId, message.PolicyId, job => job.MarkCompleted(timeProvider.GetUtcNow()), cancellationToken);

    public Task<ProcessingResultOutcome> HandleFailedAsync(ProcessPolicyFailed message, CancellationToken cancellationToken)
    {
        // The reason comes from another service; keep it within what the job can store.
        string reason = message.ErrorMessage.Length <= MaxFailureMessageLength
            ? message.ErrorMessage
            : message.ErrorMessage[..MaxFailureMessageLength];

        return RecordAsync(message.CorrelationId, message.PolicyId, job => job.MarkFailed(message.ErrorCode, reason, timeProvider.GetUtcNow()), cancellationToken);
    }

    private async Task<ProcessingResultOutcome> RecordAsync(
        Guid correlationId,
        Guid policyId,
        Func<ProcessingJob, bool> record,
        CancellationToken cancellationToken)
    {
        ProcessingJob? job = await jobs.FindByCorrelationIdAsync(correlationId, cancellationToken);

        if (job is null || job.PolicyId != policyId)
        {
            return ProcessingResultOutcome.NoMatchingJob;
        }

        if (!record(job))
        {
            return ProcessingResultOutcome.AlreadyRecorded;
        }

        await jobs.SaveChangesAsync(cancellationToken);

        return ProcessingResultOutcome.Recorded;
    }
}
