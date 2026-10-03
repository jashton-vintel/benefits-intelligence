using BenefitsIntelligence.Application.Messaging;
using BenefitsIntelligence.Domain.Processing;

namespace BenefitsIntelligence.Application.Processing;

public sealed class ProcessingResultHandler(IProcessingJobRepository jobs, TimeProvider timeProvider)
{
    public async Task<ProcessingResultOutcome> HandleCompletedAsync(ProcessPolicyCompleted message, CancellationToken cancellationToken)
    {
        ProcessingJob? job = await jobs.FindByCorrelationIdAsync(message.CorrelationId, cancellationToken);

        if (job is null || job.PolicyId != message.PolicyId)
        {
            return ProcessingResultOutcome.NoMatchingJob;
        }

        if (!job.MarkCompleted(timeProvider.GetUtcNow()))
        {
            return ProcessingResultOutcome.AlreadyRecorded;
        }

        await jobs.SaveChangesAsync(cancellationToken);

        return ProcessingResultOutcome.Recorded;
    }
}
