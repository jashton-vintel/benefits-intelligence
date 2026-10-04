namespace BenefitsIntelligence.Domain.Policies;

public sealed class BenefitPolicy
{
    private readonly List<PolicyFieldAssessment> _fieldAssessments = [];
    private readonly List<CoverageItem> _coverageItems = [];
    private readonly List<EligibilityRule> _eligibilityRules = [];

    public BenefitPolicy(
        Guid id,
        Guid organisationId,
        string name,
        BenefitType benefitType,
        Guid documentId,
        DateTimeOffset createdAt)
    {
        Id = id;
        OrganisationId = organisationId;
        Name = name;
        BenefitType = benefitType;
        DocumentId = documentId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public Guid OrganisationId { get; }

    public string Name { get; private set; }

    public BenefitType BenefitType { get; }

    public Guid DocumentId { get; }

    public DateTimeOffset CreatedAt { get; }

    public string? Provider { get; private set; }

    public string? SchemeName { get; private set; }

    public decimal? AnnualPremium { get; private set; }

    public decimal? AnnualExcess { get; private set; }

    public DateOnly? EffectiveDate { get; private set; }

    public DateOnly? RenewalDate { get; private set; }

    public bool? DependantsAllowed { get; private set; }

    public bool? DependantsIncluded { get; private set; }

    /// <summary>True when any extracted fact needs a person to confirm it.</summary>
    public bool NeedsReview { get; private set; }

    public IReadOnlyCollection<PolicyFieldAssessment> FieldAssessments => _fieldAssessments.AsReadOnly();

    public IReadOnlyCollection<CoverageItem> CoverageItems => _coverageItems.AsReadOnly();

    public IReadOnlyCollection<EligibilityRule> EligibilityRules => _eligibilityRules.AsReadOnly();

    public bool HasExtraction => _fieldAssessments.Count > 0;

    /// <summary>Replaces everything previously extracted for this policy.</summary>
    public void RecordExtraction(ExtractedPolicyFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        PolicyHeader header = facts.Header;
        RequireNotNegative(header.AnnualPremium, nameof(header.AnnualPremium));
        RequireNotNegative(header.AnnualExcess, nameof(header.AnnualExcess));
        RequireOneEach(facts.FieldAssessments.Select(a => a.Field), nameof(facts.FieldAssessments));
        RequireOneEach(facts.CoverageItems.Select(c => c.Type), nameof(facts.CoverageItems));
        RequireOneEach(facts.EligibilityRules.Select(r => r.Type), nameof(facts.EligibilityRules));

        Provider = header.Provider;
        SchemeName = header.SchemeName;
        AnnualPremium = header.AnnualPremium;
        AnnualExcess = header.AnnualExcess;
        EffectiveDate = header.EffectiveDate;
        RenewalDate = header.RenewalDate;
        DependantsAllowed = header.DependantsAllowed;
        DependantsIncluded = header.DependantsIncluded;

        Replace(_fieldAssessments, facts.FieldAssessments);
        Replace(_coverageItems, facts.CoverageItems);
        Replace(_eligibilityRules, facts.EligibilityRules);

        NeedsReview = _fieldAssessments.Any(a => a.Assessment.NeedsReview)
            || _coverageItems.Any(c => c.Assessment.NeedsReview)
            || _eligibilityRules.Any(r => r.Assessment.NeedsReview);
    }

    private static void RequireNotNegative(decimal? amount, string name)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(name, amount, "An amount cannot be negative.");
        }
    }

    private static void RequireOneEach<TKey>(IEnumerable<TKey> keys, string name)
        where TKey : struct, Enum
    {
        if (keys.GroupBy(k => k).Any(g => g.Count() > 1))
        {
            throw new ArgumentException($"Each {typeof(TKey).Name} can be recorded only once.", name);
        }
    }

    private static void Replace<T>(List<T> target, IEnumerable<T> items)
    {
        target.Clear();
        target.AddRange(items);
    }
}
