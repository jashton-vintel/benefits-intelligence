namespace BenefitsIntelligence.Domain.Policies;

public sealed class CoverageItem
{
    public const int MaxLimitLength = 500;

    public CoverageItem(CoverageType type, bool? covered, string? limit, int? sessionLimit, FactAssessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit?.Length ?? 0, MaxLimitLength, nameof(limit));

        if (sessionLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionLimit), sessionLimit, "A session limit cannot be negative.");
        }

        Id = Guid.NewGuid();
        Type = type;
        Covered = covered;
        Limit = limit;
        SessionLimit = sessionLimit;
        Assessment = assessment;
    }

    private CoverageItem()
    {
        Assessment = null!;
    }

    public Guid Id { get; private set; }

    public CoverageType Type { get; private set; }

    /// <summary>Null when the document does not say whether this is covered.</summary>
    public bool? Covered { get; private set; }

    public string? Limit { get; private set; }

    public int? SessionLimit { get; private set; }

    public FactAssessment Assessment { get; private set; }
}
