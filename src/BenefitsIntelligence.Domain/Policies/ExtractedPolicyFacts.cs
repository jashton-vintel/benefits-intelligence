namespace BenefitsIntelligence.Domain.Policies;

public sealed record ExtractedPolicyFacts(
    PolicyHeader Header,
    IReadOnlyList<PolicyFieldAssessment> FieldAssessments,
    IReadOnlyList<CoverageItem> CoverageItems,
    IReadOnlyList<EligibilityRule> EligibilityRules);
