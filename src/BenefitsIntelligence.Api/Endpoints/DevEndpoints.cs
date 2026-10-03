using BenefitsIntelligence.Application.Messaging;

namespace BenefitsIntelligence.Api.Endpoints;

internal static class DevEndpoints
{
    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/dev/ping", PingAsync);
        return app;
    }

    private static async Task<IResult> PingAsync(IProcessingRequestPublisher publisher, CancellationToken cancellationToken)
    {
        Guid policyId = Guid.NewGuid();
        ProcessPolicyRequested message = new(
            MessageId: Guid.NewGuid(),
            CorrelationId: Guid.NewGuid(),
            SchemaVersion: MessageSerialization.SchemaVersion,
            TenantId: Guid.NewGuid(),
            PolicyId: policyId,
            DocumentId: Guid.NewGuid(),
            DocumentLocation: $"documents/{policyId}.pdf",
            RequestedAt: DateTimeOffset.UtcNow);

        await publisher.PublishAsync(message, cancellationToken);

        return Results.Accepted(value: new { message.CorrelationId, message.PolicyId });
    }
}