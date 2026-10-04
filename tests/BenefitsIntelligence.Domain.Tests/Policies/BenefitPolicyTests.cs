using BenefitsIntelligence.Domain.Policies;

namespace BenefitsIntelligence.Domain.Tests.Policies;

public class BenefitPolicyTests
{
    private static readonly FactAssessment Confirmed = new(0.95, ReviewReasons.None, new Evidence(1, 1, "Atlas Healthcare Limited"));
    private static readonly FactAssessment Ambiguous = new(0.95, ReviewReasons.Ambiguous, new Evidence(2, 2, "will be confirmed at renewal"));

    private static readonly PolicyHeader Header = new(
        "Atlas Healthcare",
        "Corporate Plus",
        AnnualPremium: 120000m,
        AnnualExcess: 100m,
        EffectiveDate: new DateOnly(2026, 4, 1),
        RenewalDate: new DateOnly(2027, 4, 1),
        DependantsAllowed: true,
        DependantsIncluded: true);

    [Fact]
    public void NewPolicyHasNoExtraction()
    {
        BenefitPolicy policy = NewPolicy();

        Assert.False(policy.HasExtraction);
        Assert.False(policy.NeedsReview);
        Assert.Null(policy.Provider);
        Assert.Empty(policy.CoverageItems);
    }

    [Fact]
    public void RecordExtractionStoresHeaderAndFacts()
    {
        BenefitPolicy policy = NewPolicy();

        policy.RecordExtraction(Facts());

        Assert.True(policy.HasExtraction);
        Assert.Equal("Atlas Healthcare", policy.Provider);
        Assert.Equal(120000m, policy.AnnualPremium);
        Assert.Equal(new DateOnly(2026, 4, 1), policy.EffectiveDate);
        Assert.Equal(8, policy.FieldAssessments.Count);
        Assert.Equal(Enum.GetValues<CoverageType>(), policy.CoverageItems.Select(c => c.Type).Order());
        Assert.Equal(Enum.GetValues<EligibilityRuleType>(), policy.EligibilityRules.Select(r => r.Type).Order());
        Assert.False(policy.NeedsReview);
    }

    [Fact]
    public void PolicyNeedsReviewWhenAnyFactDoes()
    {
        BenefitPolicy policy = NewPolicy();

        policy.RecordExtraction(Facts(physiotherapy: Ambiguous));

        Assert.True(policy.NeedsReview);
    }

    [Fact]
    public void RecordingAgainReplacesThePreviousExtraction()
    {
        BenefitPolicy policy = NewPolicy();
        policy.RecordExtraction(Facts(physiotherapy: Ambiguous));

        policy.RecordExtraction(Facts() with { Header = Header with { AnnualExcess = null } });

        Assert.Null(policy.AnnualExcess);
        Assert.Equal(6, policy.CoverageItems.Count);
        Assert.False(policy.NeedsReview);
    }

    [Theory]
    [InlineData(-1, 100)]
    [InlineData(120000, -1)]
    public void RecordExtractionRejectsNegativeAmountsAndLeavesPolicyUnchanged(decimal premium, decimal excess)
    {
        BenefitPolicy policy = NewPolicy();

        ExtractedPolicyFacts facts = Facts() with { Header = Header with { AnnualPremium = premium, AnnualExcess = excess } };

        Assert.Throws<ArgumentOutOfRangeException>(() => policy.RecordExtraction(facts));
        Assert.False(policy.HasExtraction);
        Assert.Null(policy.Provider);
    }

    [Fact]
    public void RecordExtractionRejectsTheSameCoverageTwice()
    {
        BenefitPolicy policy = NewPolicy();
        ExtractedPolicyFacts facts = Facts();

        ExtractedPolicyFacts duplicated = facts with { CoverageItems = [.. facts.CoverageItems, Coverage(CoverageType.Cancer, Confirmed)] };

        Assert.Throws<ArgumentException>(() => policy.RecordExtraction(duplicated));
    }

    private static ExtractedPolicyFacts Facts(FactAssessment? physiotherapy = null) =>
        new(
            Header,
            Enum.GetValues<PolicyField>().Select(field => new PolicyFieldAssessment(field, Confirmed)).ToList(),
            Enum.GetValues<CoverageType>()
                .Select(type => Coverage(type, type == CoverageType.Physiotherapy ? physiotherapy ?? Confirmed : Confirmed))
                .ToList(),
            Enum.GetValues<EligibilityRuleType>().Select(type => new EligibilityRule(type, "Permanent", Confirmed)).ToList());

    private static CoverageItem Coverage(CoverageType type, FactAssessment assessment) =>
        new(type, covered: true, limit: "Paid in full", sessionLimit: null, assessment);

    private static BenefitPolicy NewPolicy() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Current policy", BenefitType.PrivateMedicalInsurance, Guid.NewGuid(), DateTimeOffset.UtcNow);
}
