using BenefitsIntelligence.Domain.Policies;

namespace BenefitsIntelligence.Application.Policies;

public sealed record CoverageDetail(
    CoverageType Type,
    bool? Covered,
    string? Limit,
    int? SessionLimit,
    AssessmentDetail Assessment);
