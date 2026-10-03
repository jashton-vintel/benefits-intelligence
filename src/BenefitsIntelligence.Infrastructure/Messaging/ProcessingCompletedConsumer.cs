using System.Text.Json;

using BenefitsIntelligence.Application.Messaging;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BenefitsIntelligence.Infrastructure.Messaging;

internal sealed partial class ProcessingCompletedConsumer(
    RabbitMqConnection connection,
    ILogger<ProcessingCompletedConsumer> logger) : BackgroundService
{
    private const ushort PrefetchCount = 10;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IConnection rabbit = await connection.GetAsync(stoppingToken);

        await using IChannel channel = await rabbit.CreateChannelAsync(cancellationToken: stoppingToken);

        channel.CallbackExceptionAsync += (_, args) =>
        {
            LogCallbackFailed(args.Exception);
            return Task.CompletedTask;
        };

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: PrefetchCount, global: false, stoppingToken);

        AsyncEventingBasicConsumer consumer = new(channel);

        consumer.ReceivedAsync += (_, delivery) => HandleAsync(channel, delivery);

        await channel.BasicConsumeAsync(RabbitMqTopology.ProcessedQueue, autoAck: false, consumer, stoppingToken);

        LogConsuming(RabbitMqTopology.ProcessedQueue);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task HandleAsync(IChannel channel, BasicDeliverEventArgs delivery)
    {
        try
        {
            ProcessPolicyCompleted message =
                JsonSerializer.Deserialize<ProcessPolicyCompleted>(delivery.Body.Span, MessageSerialization.Options)
                ?? throw new JsonException("Message body was null.");

            LogCompleted(message.PolicyId, message.CorrelationId);
        }
        catch (Exception exception)
        {
            LogFailed(exception, delivery.BasicProperties.MessageId);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, delivery.CancellationToken);
            return;
        }

        try
        {
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, delivery.CancellationToken);
        }
        catch (Exception exception)
        {
            // Typically the channel closed after handling succeeded. The broker still holds the message as unacked and will redeliver it, so handling must be idempotent.
            LogAckFailed(exception, delivery.BasicProperties.MessageId);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Consuming from {Queue}")]
    private partial void LogConsuming(string queue);

    [LoggerMessage(Level = LogLevel.Information, Message = "Policy {PolicyId} processing completed (correlation_id={CorrelationId})")]
    private partial void LogCompleted(Guid policyId, Guid correlationId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to handle message {MessageId}; rejected without requeue")]
    private partial void LogFailed(Exception exception, string? messageId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception in RabbitMQ consumer callback")]
    private partial void LogCallbackFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to acknowledge message {MessageId}; it will be redelivered")]
    private partial void LogAckFailed(Exception exception, string? messageId);
}