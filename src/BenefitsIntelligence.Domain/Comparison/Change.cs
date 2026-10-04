namespace BenefitsIntelligence.Domain.Comparison;

public enum Change
{
    Unchanged,
    Increased,
    Decreased,
    Changed,

    /// <summary>A requirement the current policy does not have is set by the proposed one.</summary>
    Added,

    /// <summary>A requirement the current policy has is not set by the proposed one.</summary>
    Removed,

    /// <summary>At least one side has no value, so the two cannot be compared.</summary>
    Unknown,
}
