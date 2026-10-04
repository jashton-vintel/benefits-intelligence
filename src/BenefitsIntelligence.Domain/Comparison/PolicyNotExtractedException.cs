namespace BenefitsIntelligence.Domain.Comparison;

public sealed class PolicyNotExtractedException : InvalidOperationException
{
    public PolicyNotExtractedException(Guid policyId)
        : base($"Policy {policyId} has not finished processing, so it cannot be compared.")
    {
        PolicyId = policyId;
    }

    public Guid PolicyId { get; }
}
