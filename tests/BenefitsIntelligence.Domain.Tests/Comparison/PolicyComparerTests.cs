using BenefitsIntelligence.Domain.Comparison;
using BenefitsIntelligence.Domain.Policies;

namespace BenefitsIntelligence.Domain.Tests.Comparison;

public class PolicyComparerTests
{
    private static readonly FactAssessment Confirmed = new(0.95, ReviewReasons.None, new Evidence(1, 1, "quoted text"));
    private static readonly FactAssessment Ambiguous = new(0.95, ReviewReasons.Ambiguous, new Evidence(2, 2, "will be confirmed at renewal"));

    private static readonly PolicyHeader CurrentHeader = new(
        "Atlas Healthcare",
        "Corporate Plus",
        AnnualPremium: 120000m,
        AnnualExcess: 100m,
        EffectiveDate: new DateOnly(2026, 4, 1),
        RenewalDate: new DateOnly(2027, 4, 1),
        DependantsAllowed: true,
        DependantsIncluded: true);

    private static readonly PolicyHeader ProposedHeader = new(
        "NorthStar Health",
        "Essentials Select",
        AnnualPremium: 108000m,
        AnnualExcess: 150m,
        EffectiveDate: new DateOnly(2027, 4, 1),
        RenewalDate: new DateOnly(2028, 4, 1),
        DependantsAllowed: true,
        DependantsIncluded: null);

    [Fact]
    public void ReportsLowerPremiumAsADecreaseWithItsDelta()
    {
        FieldDifference premium = Difference(Compare(), "annual_premium");

        Assert.Equal(new FieldDifference("annual_premium", ValueKind.Money, "120000.00", "108000.00", -12000m, Change.Decreased, false), premium);
    }

    [Fact]
    public void ReportsHigherExcessAsAnIncrease()
    {
        FieldDifference excess = Difference(Compare(), "annual_excess");

        Assert.Equal(Change.Increased, excess.Change);
        Assert.Equal(50m, excess.Delta);
    }

    [Fact]
    public void ComparesSessionLimits()
    {
        FieldDifference sessions = Difference(Compare(), "coverage.physiotherapy.sessions");

        Assert.Equal(("8", "10", 2m, Change.Increased), (sessions.Current, sessions.Proposed, sessions.Delta, sessions.Change));
    }

    [Fact]
    public void OmitsSessionComparisonWhenNeitherPolicyLimitsSessions()
    {
        PolicyComparison comparison = Compare();

        Assert.DoesNotContain(comparison.Differences, d => d.Field == "coverage.cancer.sessions");
        Assert.Contains(comparison.Differences, d => d.Field == "coverage.cancer.covered");
    }

    [Fact]
    public void SameCoverIsUnchanged()
    {
        FieldDifference covered = Difference(Compare(), "coverage.mental_health.covered");

        Assert.Equal(("true", "true", Change.Unchanged), (covered.Current, covered.Proposed, covered.Change));
    }

    [Fact]
    public void DifferentWordingIsReportedAsChangedNotInterpreted()
    {
        FieldDifference limit = Difference(Compare(), "coverage.inpatient.limit");

        Assert.Equal(Change.Changed, limit.Change);
        Assert.Null(limit.Delta);
    }

    [Fact]
    public void TextThatDiffersOnlyInCaseOrSpacingIsUnchanged()
    {
        BenefitPolicy proposed = Policy(ProposedHeader with { Provider = "  atlas   HEALTHCARE " });

        FieldDifference provider = Difference(PolicyComparer.Compare(Policy(CurrentHeader), proposed), "provider");

        Assert.Equal(Change.Unchanged, provider.Change);
    }

    [Fact]
    public void MissingValueCannotBeCompared()
    {
        FieldDifference dependants = Difference(Compare(), "dependants_included");

        Assert.Equal(("true", null, Change.Unknown), (dependants.Current, dependants.Proposed, dependants.Change));
    }

    [Fact]
    public void DifferenceIsFlaggedWhenEitherSideNeedsReview()
    {
        BenefitPolicy proposed = Policy(ProposedHeader, reviewed: (PolicyField.DependantsIncluded, Ambiguous));

        PolicyComparison comparison = PolicyComparer.Compare(Policy(CurrentHeader), proposed);

        Assert.True(Difference(comparison, "dependants_included").NeedsReview);
        Assert.False(Difference(comparison, "annual_premium").NeedsReview);
    }

    [Fact]
    public void ComparesServiceRequirementAsANumberOfMonths()
    {
        FieldDifference service = Difference(Compare(), "eligibility.minimum_service_months");

        Assert.Equal((ValueKind.Count, -3m, Change.Decreased), (service.Kind, service.Delta, service.Change));
    }

    [Fact]
    public void RequirementTheProposedPolicyDropsIsRemoved()
    {
        BenefitPolicy current = Policy(CurrentHeader, grade: ("Grade 4", Confirmed));
        BenefitPolicy proposed = Policy(ProposedHeader, grade: (null, Confirmed));

        FieldDifference grade = Difference(PolicyComparer.Compare(current, proposed), "eligibility.minimum_grade");

        Assert.Equal(("Grade 4", null, Change.Removed), (grade.Current, grade.Proposed, grade.Change));
    }

    [Fact]
    public void RequirementTheProposedPolicyIntroducesIsAdded()
    {
        BenefitPolicy current = Policy(CurrentHeader, grade: (null, Confirmed));
        BenefitPolicy proposed = Policy(ProposedHeader, grade: ("Grade 4", Confirmed));

        FieldDifference grade = Difference(PolicyComparer.Compare(current, proposed), "eligibility.minimum_grade");

        Assert.Equal(Change.Added, grade.Change);
    }

    [Fact]
    public void SilenceOrAmbiguityIsNotTreatedAsNoRequirement()
    {
        FactAssessment unquoted = new(0.95, ReviewReasons.None, evidence: null);
        BenefitPolicy current = Policy(CurrentHeader, grade: ("Grade 4", Confirmed));

        Change silent = Difference(PolicyComparer.Compare(current, Policy(ProposedHeader, grade: (null, unquoted))), "eligibility.minimum_grade").Change;
        Change ambiguous = Difference(PolicyComparer.Compare(current, Policy(ProposedHeader, grade: (null, Ambiguous))), "eligibility.minimum_grade").Change;

        Assert.Equal((Change.Unknown, Change.Unknown), (silent, ambiguous));
    }

    [Fact]
    public void ReportsEveryFieldOnce()
    {
        PolicyComparison comparison = Compare();

        Assert.Equal(comparison.Differences.Count, comparison.Differences.Select(d => d.Field).Distinct().Count());
        Assert.Equal(8 + (6 * 2) + 1 + 4, comparison.Differences.Count);
    }

    [Fact]
    public void PolicyThatHasNotBeenExtractedCannotBeCompared()
    {
        BenefitPolicy unprocessed = NewPolicy();

        PolicyNotExtractedException exception = Assert.Throws<PolicyNotExtractedException>(
            () => PolicyComparer.Compare(Policy(CurrentHeader), unprocessed));

        Assert.Equal(unprocessed.Id, exception.PolicyId);
    }

    private static PolicyComparison Compare() =>
        PolicyComparer.Compare(Policy(CurrentHeader, sessions: 8, serviceMonths: "3"), Policy(ProposedHeader, sessions: 10, serviceMonths: "0"));

    private static FieldDifference Difference(PolicyComparison comparison, string field) =>
        comparison.Differences.Single(d => d.Field == field);

    private static BenefitPolicy Policy(
        PolicyHeader header,
        int sessions = 8,
        string serviceMonths = "3",
        (PolicyField Field, FactAssessment Assessment)? reviewed = null,
        (string? Value, FactAssessment Assessment)? grade = null)
    {
        BenefitPolicy policy = NewPolicy();

        policy.RecordExtraction(new ExtractedPolicyFacts(
            header,
            Enum.GetValues<PolicyField>()
                .Select(field => new PolicyFieldAssessment(field, field == reviewed?.Field ? reviewed.Value.Assessment : Confirmed))
                .ToList(),
            Enum.GetValues<CoverageType>()
                .Select(type => new CoverageItem(
                    type,
                    covered: true,
                    limit: $"{header.Provider} {type} terms",
                    sessionLimit: type == CoverageType.Physiotherapy ? sessions : null,
                    Confirmed))
                .ToList(),
            Enum.GetValues<EligibilityRuleType>()
                .Select(type => type switch
                {
                    EligibilityRuleType.MinimumServiceMonths => new EligibilityRule(type, serviceMonths, Confirmed),
                    EligibilityRuleType.MinimumGrade when grade is { } g => new EligibilityRule(type, g.Value, g.Assessment),
                    _ => new EligibilityRule(type, "Permanent", Confirmed),
                })
                .ToList()));

        return policy;
    }

    private static BenefitPolicy NewPolicy() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Policy", BenefitType.PrivateMedicalInsurance, Guid.NewGuid(), DateTimeOffset.UtcNow);
}
