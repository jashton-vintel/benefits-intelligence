namespace BenefitsIntelligence.Application.Messaging;

public sealed record ProcessPolicyFailed(
    Guid MessageId,
    Guid CorrelationId,
    string SchemaVersion,
    Guid TenantId,
    Guid PolicyId,
    string Status,
    string ErrorCode,
    string ErrorMessage);
