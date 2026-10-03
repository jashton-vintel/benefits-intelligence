using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BenefitsIntelligence.Infrastructure.Messaging;

public sealed partial class RabbitMqConnection(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConnection> logger) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
        {
            return _connection;
        }

        // Serialises first-time creation so concurrent callers can't each open (and leak) a connection
        // SemaphoreSlim rather than lock because the critical section awaits
        await _gate.WaitAsync(cancellationToken);

        try
        {
            _connection ??= await ConnectAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }

        _gate.Dispose();
    }

    private async Task<IConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        RabbitMqOptions settings = options.Value;

        ConnectionFactory factory = new()
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            ClientProvidedName = "benefits-api",
        };

        IConnection connection = await factory.CreateConnectionAsync(cancellationToken);

        connection.ConnectionShutdownAsync += OnShutdownAsync;
        connection.RecoverySucceededAsync += OnRecoverySucceededAsync;
        connection.ConnectionRecoveryErrorAsync += OnRecoveryErrorAsync;

        LogConnected(settings.HostName, settings.Port);

        return connection;
    }

    private Task OnShutdownAsync(object sender, ShutdownEventArgs args)
    {
        if (args.Initiator == ShutdownInitiator.Application)
        {
            LogClosed();
        }
        else
        {
            LogConnectionLost(args.Initiator, args.ReplyCode, args.ReplyText);
        }

        return Task.CompletedTask;
    }

    private Task OnRecoverySucceededAsync(object sender, AsyncEventArgs args)
    {
        LogRecovered();
        return Task.CompletedTask;
    }

    private Task OnRecoveryErrorAsync(object sender, ConnectionRecoveryErrorEventArgs args)
    {
        // Failures repeat every recovery interval while the broker is down; the root cause is
        // enough to diagnose without a stack trace per attempt and swamping the logs!
        LogRecoveryFailed(args.Exception.GetBaseException().Message);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Connected to RabbitMQ at {HostName}:{Port}")]
    private partial void LogConnected(string hostName, int port);

    [LoggerMessage(Level = LogLevel.Information, Message = "RabbitMQ connection closed")]
    private partial void LogClosed();

    [LoggerMessage(Level = LogLevel.Warning, Message = "RabbitMQ connection lost ({Initiator}, {ReplyCode}: {ReplyText}); attempting recovery")]
    private partial void LogConnectionLost(ShutdownInitiator initiator, ushort replyCode, string replyText);

    [LoggerMessage(Level = LogLevel.Information, Message = "RabbitMQ connection recovered")]
    private partial void LogRecovered();

    [LoggerMessage(Level = LogLevel.Warning, Message = "RabbitMQ connection recovery attempt failed: {Reason}; retrying")]
    private partial void LogRecoveryFailed(string reason);
}
