namespace BenefitsIntelligence.Domain.Processing;

public sealed class InvalidStatusTransitionException : InvalidOperationException
{
    public InvalidStatusTransitionException(ProcessingStatus from, ProcessingStatus to)
        : base($"Processing job cannot move from {from} to {to}.")
    {
        From = from;
        To = to;
    }

    public ProcessingStatus From { get; }

    public ProcessingStatus To { get; }
}
