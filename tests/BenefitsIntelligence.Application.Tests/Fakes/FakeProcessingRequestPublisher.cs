using BenefitsIntelligence.Application.Messaging;

namespace BenefitsIntelligence.Application.Tests.Fakes;

internal sealed class FakeProcessingRequestPublisher(CallLog log) : IProcessingRequestPublisher
{
    public List<ProcessPolicyRequested> Published { get; } = [];

    public Exception? FailWith { get; set; }

    public Task PublishAsync(ProcessPolicyRequested message, CancellationToken cancellationToken)
    {
        log.Record("publish");

        if (FailWith is not null)
        {
            return Task.FromException(FailWith);
        }

        Published.Add(message);
        return Task.CompletedTask;
    }
}
