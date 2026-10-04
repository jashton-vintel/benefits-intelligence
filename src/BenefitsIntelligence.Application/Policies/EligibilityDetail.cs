using BenefitsIntelligence.Domain.Policies;

namespace BenefitsIntelligence.Application.Policies;

public sealed record EligibilityDetail(EligibilityRuleType Type, string? Value, AssessmentDetail Assessment);
