using System.Text.Json;

using BenefitsIntelligence.Application.Messaging;

using Microsoft.Extensions.Logging;

using RabbitMQ.Client;

namespace BenefitsIntelligence.Infrastructure.Messaging;

internal sealed partial class RabbitMqProcessingRequestPublisher(
    RabbitMqConnection connection,
    ILogger<RabbitMqProcessingRequestPublisher> logger) : IProcessingRequestPublisher
{
    // With confirmation tracking, BasicPublishAsync completes only once the broker has accepted the message and throws if it is nacked or unroutable
    private static readonly CreateChannelOptions ConfirmedChannel = new(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true);

    public async Task PublishAsync(ProcessPolicyRequested message, CancellationToken cancellationToken)
    {
        string messageId = message.MessageId.ToString();
        string correlationId = message.CorrelationId.ToString();

        using IDisposable? scope = MessageLogScope.Begin(logger, correlationId, messageId);

        IConnection rabbit = await connection.GetAsync(cancellationToken);

        await using IChannel channel = await rabbit.CreateChannelAsync(ConfirmedChannel, cancellationToken);

        BasicProperties properties = new()
        {
            MessageId = messageId,
            CorrelationId = correlationId,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Type = RabbitMqTopology.ProcessRequested,
        };

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message, MessageSerialization.Options);

        await channel.BasicPublishAsync(
            RabbitMqTopology.Exchange,
            RabbitMqTopology.ProcessRequested,
            mandatory: true,
            properties,
            body,
            cancellationToken);

        LogPublished(message.PolicyId);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Published processing request for policy {PolicyId}")]
    private partial void LogPublished(Guid policyId);
}
