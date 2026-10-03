using BenefitsIntelligence.Application.Messaging;
using BenefitsIntelligence.Infrastructure.Messaging;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BenefitsIntelligence.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.UserName) && !string.IsNullOrWhiteSpace(o.Password),
                "RabbitMq:UserName and RabbitMq:Password must be configured.")
            .ValidateOnStart();

        services.AddSingleton<RabbitMqConnection>();
        services.AddHostedService<RabbitMqTopologyInitializer>();
        services.AddHostedService<ProcessingCompletedConsumer>();
        services.AddSingleton<IProcessingRequestPublisher, RabbitMqProcessingRequestPublisher>();

        return services;
    }
}