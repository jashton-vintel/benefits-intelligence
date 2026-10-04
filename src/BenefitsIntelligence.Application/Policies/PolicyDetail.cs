using BenefitsIntelligence.Domain.Policies;
using BenefitsIntelligence.Domain.Processing;

namespace BenefitsIntelligence.Application.Policies;

public sealed record PolicyDetail(
    Guid Id,
    string Name,
    BenefitType BenefitType,
    string FileName,
    DateTimeOffset CreatedAt,
    ProcessingStatus Status,
    string? FailureCode,
    string? FailureMessage,
    DateTimeOffset StatusUpdatedAt,
    ExtractionDetail? Extraction);
