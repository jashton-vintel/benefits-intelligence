using System.Text.Json;

namespace BenefitsIntelligence.Application.Messaging;

public sealed record ProcessPolicyCompleted(
    Guid MessageId,
    Guid CorrelationId,
    string SchemaVersion,
    Guid TenantId,
    Guid PolicyId,
    string Status,
    JsonElement Extraction);