namespace BenefitsIntelligence.Application.Messaging;

public sealed record ProcessPolicyCompleted(
    Guid MessageId,
    Guid CorrelationId,
    string SchemaVersion,
    Guid TenantId,
    Guid PolicyId,
    string Status,
    DocumentSummary Document,
    PolicyExtraction Extraction);
