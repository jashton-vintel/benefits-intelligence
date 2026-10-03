using System.Text.Json;

using BenefitsIntelligence.Application.Messaging;
using BenefitsIntelligence.Application.Processing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BenefitsIntelligence.Infrastructure.Messaging;

internal sealed partial class ProcessingCompletedConsumer(
    RabbitMqConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<ProcessingCompletedConsumer> logger) : BackgroundService
{
    private const ushort PrefetchCount = 10;

    private enum Settlement
    {
        Ack,
        Requeue,
        Reject,
    }

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
        using IDisposable? scope = MessageLogScope.Begin(logger, delivery.BasicProperties.CorrelationId, delivery.BasicProperties.MessageId);

        Settlement settlement;

        try
        {
            settlement = await ProcessAsync(delivery);
        }
        catch (Exception exception) when (TransientFailure.IsTransient(exception))
        {
            LogTransientFailure(exception);
            settlement = Settlement.Requeue;
        }
        catch (Exception exception)
        {
            LogFailed(exception);
            settlement = Settlement.Reject;
        }

        await SettleAsync(channel, delivery, settlement);
    }

    private async Task<Settlement> ProcessAsync(BasicDeliverEventArgs delivery)
    {
        ProcessPolicyCompleted message =
            JsonSerializer.Deserialize<ProcessPolicyCompleted>(delivery.Body.Span, MessageSerialization.Options)
            ?? throw new JsonException("Message body was null.");

        using IDisposable? policyScope = logger.BeginScope(new Dictionary<string, object?> { ["policy_id"] = message.PolicyId });

        // Scoped services (DbContext in particular) must not outlive a single message.
        await using AsyncServiceScope services = scopeFactory.CreateAsyncScope();
        ProcessingResultHandler handler = services.ServiceProvider.GetRequiredService<ProcessingResultHandler>();

        ProcessingResultOutcome outcome = await handler.HandleCompletedAsync(message, delivery.CancellationToken);

        switch (outcome)
        {
            case ProcessingResultOutcome.Recorded:
                LogCompleted(message.PolicyId);
                return Settlement.Ack;
            case ProcessingResultOutcome.AlreadyRecorded:
                LogDuplicate(message.PolicyId);
                return Settlement.Ack;
            default:
                LogNoMatchingJob(message.PolicyId);
                return Settlement.Reject;
        }
    }

    private async Task SettleAsync(IChannel channel, BasicDeliverEventArgs delivery, Settlement settlement)
    {
        try
        {
            if (settlement == Settlement.Ack)
            {
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, delivery.CancellationToken);
            }
            else
            {
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: settlement == Settlement.Requeue, delivery.CancellationToken);
            }
        }
        catch (Exception exception)
        {
            // Typically the channel closed after handling finished. The broker still holds the message as unacked and
            // will redeliver it, so handling must be idempotent.
            LogSettleFailed(exception, settlement.ToString());
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Consuming from {Queue}")]
    private partial void LogConsuming(string queue);

    [LoggerMessage(Level = LogLevel.Information, Message = "Policy {PolicyId} processing completed")]
    private partial void LogCompleted(Guid policyId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Policy {PolicyId} completion already recorded; duplicate delivery ignored")]
    private partial void LogDuplicate(Guid policyId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No processing job matches completion for policy {PolicyId}; rejected")]
    private partial void LogNoMatchingJob(Guid policyId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Transient failure handling message; requeued for redelivery")]
    private partial void LogTransientFailure(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to handle message; rejected without requeue")]
    private partial void LogFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception in RabbitMQ consumer callback")]
    private partial void LogCallbackFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to settle message as {Settlement}; it will be redelivered")]
    private partial void LogSettleFailed(Exception exception, string settlement);
}
