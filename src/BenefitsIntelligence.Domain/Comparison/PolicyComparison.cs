namespace BenefitsIntelligence.Domain.Comparison;

public sealed record PolicyComparison(Guid CurrentPolicyId, Guid ProposedPolicyId, IReadOnlyList<FieldDifference> Differences);
