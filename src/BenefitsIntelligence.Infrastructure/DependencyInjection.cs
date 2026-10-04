using BenefitsIntelligence.Application.Documents;
using BenefitsIntelligence.Application.Messaging;
using BenefitsIntelligence.Application.Policies;
using BenefitsIntelligence.Application.Processing;
using BenefitsIntelligence.Infrastructure.Documents;
using BenefitsIntelligence.Infrastructure.Messaging;
using BenefitsIntelligence.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BenefitsIntelligence.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "BenefitsIntelligence";

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddScoped<IPolicyRepository, EfPolicyRepository>();
        services.AddScoped<IPolicyQueries, EfPolicyQueries>();
        services.AddScoped<IProcessingJobRepository, EfProcessingJobRepository>();

        return services;
    }

    public static IServiceCollection AddDocumentStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DocumentStorageOptions>()
            .Bind(configuration.GetSection(DocumentStorageOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.RootPath), "DocumentStorage:RootPath must be configured.")
            .ValidateOnStart();

        services.AddSingleton<IDocumentStore>(provider =>
        {
            DocumentStorageOptions options = provider.GetRequiredService<IOptions<DocumentStorageOptions>>().Value;
            IHostEnvironment environment = provider.GetRequiredService<IHostEnvironment>();

            return new LocalDocumentStore(Path.GetFullPath(options.RootPath, environment.ContentRootPath));
        });

        return services;
    }

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
        services.AddHostedService<ProcessingResultConsumer>();
        services.AddSingleton<IProcessingRequestPublisher, RabbitMqProcessingRequestPublisher>();

        return services;
    }
}