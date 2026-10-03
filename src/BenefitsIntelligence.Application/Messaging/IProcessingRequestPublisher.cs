namespace BenefitsIntelligence.Application.Messaging;

public interface IProcessingRequestPublisher
{
    Task PublishAsync(ProcessPolicyRequested message, CancellationToken cancellationToken);
}