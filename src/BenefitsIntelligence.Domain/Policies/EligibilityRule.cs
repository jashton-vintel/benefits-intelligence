namespace BenefitsIntelligence.Domain.Policies;

public sealed class EligibilityRule
{
    public const int MaxValueLength = 500;

    public EligibilityRule(EligibilityRuleType type, string? value, FactAssessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value?.Length ?? 0, MaxValueLength, nameof(value));

        Id = Guid.NewGuid();
        Type = type;
        Value = value;
        Assessment = assessment;
    }

    private EligibilityRule()
    {
        Assessment = null!;
    }

    public Guid Id { get; private set; }

    public EligibilityRuleType Type { get; private set; }

    /// <summary>Null when the document sets no requirement of this type.</summary>
    public string? Value { get; private set; }

    public FactAssessment Assessment { get; private set; }
}
