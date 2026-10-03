using Microsoft.Extensions.Hosting;

using RabbitMQ.Client;

namespace BenefitsIntelligence.Infrastructure.Messaging;

internal sealed class RabbitMqTopologyInitializer(RabbitMqConnection connection) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        IConnection rabbit = await connection.GetAsync(cancellationToken);
        await using IChannel channel = await rabbit.CreateChannelAsync(cancellationToken: cancellationToken);
        await RabbitMqTopology.DeclareAsync(channel, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}