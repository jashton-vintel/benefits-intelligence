namespace BenefitsIntelligence.Application.Comparison;

/// <param name="PolicyId">The policy that was missing or not ready, when the comparison could not be made.</param>
public sealed record ComparisonResult(ComparisonStatus Status, ComparisonReport? Report = null, Guid? PolicyId = null)
{
    public static ComparisonResult NotFound(Guid policyId) => new(ComparisonStatus.PolicyNotFound, PolicyId: policyId);

    public static ComparisonResult NotReady(Guid policyId) => new(ComparisonStatus.PolicyNotReady, PolicyId: policyId);
}
