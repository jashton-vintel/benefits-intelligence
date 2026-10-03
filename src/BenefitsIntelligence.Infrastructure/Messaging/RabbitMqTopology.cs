using RabbitMQ.Client;

namespace BenefitsIntelligence.Infrastructure.Messaging;

internal static class RabbitMqTopology
{
    public const string Exchange = "benefits.events";
    public const string ProcessingQueue = "policy.processing";
    public const string ProcessedQueue = "policy.processed";
    public const string ProcessRequested = "policy.process.requested";
    public const string ProcessCompleted = "policy.process.completed";

    // Must match the Python worker's declarations exactly.. a mismatch fails with PRECONDITION_FAILED.
    public static async Task DeclareAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: cancellationToken);

        await DeclareBoundQueueAsync(channel, ProcessingQueue, ProcessRequested, cancellationToken);
        await DeclareBoundQueueAsync(channel, ProcessedQueue, ProcessCompleted, cancellationToken);
    }

    private static async Task DeclareBoundQueueAsync(IChannel channel, string queue, string routingKey, CancellationToken cancellationToken)
    {
        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(queue, Exchange, routingKey, cancellationToken: cancellationToken);
    }
}