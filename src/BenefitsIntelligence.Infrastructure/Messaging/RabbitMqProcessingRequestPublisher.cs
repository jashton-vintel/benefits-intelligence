using System.Text.Json;

using BenefitsIntelligence.Application.Messaging;

using RabbitMQ.Client;

namespace BenefitsIntelligence.Infrastructure.Messaging;

internal sealed class RabbitMqProcessingRequestPublisher(RabbitMqConnection connection) : IProcessingRequestPublisher
{
    // With confirmation tracking, BasicPublishAsync completes only once the broker has accepted the message and throws if it is nacked or unroutable
    private static readonly CreateChannelOptions ConfirmedChannel = new(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true);

    public async Task PublishAsync(ProcessPolicyRequested message, CancellationToken cancellationToken)
    {
        IConnection rabbit = await connection.GetAsync(cancellationToken);

        await using IChannel channel = await rabbit.CreateChannelAsync(ConfirmedChannel, cancellationToken);

        BasicProperties properties = new()
        {
            MessageId = message.MessageId.ToString(),
            CorrelationId = message.CorrelationId.ToString(),
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
    }
}