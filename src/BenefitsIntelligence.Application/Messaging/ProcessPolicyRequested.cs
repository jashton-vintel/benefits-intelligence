namespace BenefitsIntelligence.Application.Messaging;

public sealed record ProcessPolicyRequested(
    Guid MessageId,
    Guid CorrelationId,
    string SchemaVersion,
    Guid TenantId,
    Guid PolicyId,
    Guid DocumentId,
    string DocumentLocation,
    DateTimeOffset RequestedAt);