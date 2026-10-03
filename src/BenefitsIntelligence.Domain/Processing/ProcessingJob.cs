namespace BenefitsIntelligence.Domain.Processing;

/// <summary>
/// One attempt at processing a policy document. Messages are delivered at least once and
/// the worker can finish before the API records that the job was queued, so transitions
/// are written to tolerate duplicates and that ordering.
/// </summary>
public sealed class ProcessingJob
{
    private ProcessingJob(
        Guid id,
        Guid policyId,
        Guid correlationId,
        ProcessingStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        PolicyId = policyId;
        CorrelationId = correlationId;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    public Guid PolicyId { get; }

    public Guid CorrelationId { get; }

    public ProcessingStatus Status { get; private set; }

    public string? FailureCode { get; private set; }

    public string? FailureMessage { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsFinished => Status is ProcessingStatus.Completed or ProcessingStatus.Failed;

    public static ProcessingJob Create(Guid policyId, DateTimeOffset now) => new(Guid.NewGuid(), policyId, Guid.NewGuid(), ProcessingStatus.Uploaded, now, now);

    public bool MarkQueued(DateTimeOffset now)
    {        
        if (Status != ProcessingStatus.Uploaded)
        {
            return false;
        }

        MoveTo(ProcessingStatus.Queued, now);
        return true;
    }

    public bool MarkCompleted(DateTimeOffset now)
    {
        switch (Status)
        {
            case ProcessingStatus.Completed:
                return false;
            case ProcessingStatus.Failed:
                throw new InvalidStatusTransitionException(Status, ProcessingStatus.Completed);
            default:
                MoveTo(ProcessingStatus.Completed, now);
                return true;
        }
    }
    
    public bool MarkFailed(string code, string message, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        switch (Status)
        {
            case ProcessingStatus.Failed:
                return false;
            case ProcessingStatus.Completed:
                throw new InvalidStatusTransitionException(Status, ProcessingStatus.Failed);
            default:
                FailureCode = code;
                FailureMessage = message;
                MoveTo(ProcessingStatus.Failed, now);
                return true;
        }
    }

    private void MoveTo(ProcessingStatus status, DateTimeOffset now)
    {
        Status = status;
        UpdatedAt = now;
    }
}
