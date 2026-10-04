using BenefitsIntelligence.Domain.Policies;
using BenefitsIntelligence.Domain.Processing;

namespace BenefitsIntelligence.Application.Policies;

public sealed record PolicySummary(
    Guid Id,
    string Name,
    BenefitType BenefitType,
    string FileName,
    DateTimeOffset CreatedAt,
    string? Provider,
    string? SchemeName,
    decimal? AnnualExcess,
    ProcessingStatus Status,
    string? FailureCode,
    string? FailureMessage,
    DateTimeOffset StatusUpdatedAt);
