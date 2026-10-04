using System.Globalization;

using BenefitsIntelligence.Application.Messaging;
using BenefitsIntelligence.Domain.Policies;

namespace BenefitsIntelligence.Application.Processing;

internal static class ExtractionMapper
{
    public static ExtractedPolicyFacts ToFacts(PolicyExtraction extraction, ReviewPolicy review)
    {
        if (extraction.SchemaVersion != PolicyExtraction.SupportedSchemaVersion)
        {
            throw new NotSupportedException($"Extraction schema version '{extraction.SchemaVersion}' is not supported.");
        }

        PolicyHeader header = new(
            extraction.Provider.Value,
            extraction.SchemeName.Value,
            extraction.AnnualPremium.Value,
            extraction.AnnualExcess.Value,
            extraction.EffectiveDate.Value,
            extraction.RenewalDate.Value,
            extraction.DependantsAllowed.Value,
            extraction.DependantsIncluded.Value);

        PolicyFieldAssessment[] fields =
        [
            new(PolicyField.Provider, Assess(extraction.Provider, review)),
            new(PolicyField.SchemeName, Assess(extraction.SchemeName, review)),
            new(PolicyField.AnnualPremium, Assess(extraction.AnnualPremium, review)),
            new(PolicyField.AnnualExcess, Assess(extraction.AnnualExcess, review)),
            new(PolicyField.EffectiveDate, Assess(extraction.EffectiveDate, review)),
            new(PolicyField.RenewalDate, Assess(extraction.RenewalDate, review)),
            new(PolicyField.DependantsAllowed, Assess(extraction.DependantsAllowed, review)),
            new(PolicyField.DependantsIncluded, Assess(extraction.DependantsIncluded, review)),
        ];

        ExtractedCoverage coverage = extraction.Coverage;
        CoverageItem[] coverageItems =
        [
            ToCoverageItem(CoverageType.Inpatient, coverage.Inpatient, review),
            ToCoverageItem(CoverageType.Outpatient, coverage.Outpatient, review),
            ToCoverageItem(CoverageType.Diagnostics, coverage.Diagnostics, review),
            ToCoverageItem(CoverageType.Physiotherapy, coverage.Physiotherapy, review),
            ToCoverageItem(CoverageType.MentalHealth, coverage.MentalHealth, review),
            ToCoverageItem(CoverageType.Cancer, coverage.Cancer, review),
        ];

        ExtractedEligibility eligibility = extraction.Eligibility;
        EligibilityRule[] rules =
        [
            new(EligibilityRuleType.EmploymentType, eligibility.EmploymentType.Value, Assess(eligibility.EmploymentType, review)),
            new(EligibilityRuleType.Country, eligibility.Country.Value, Assess(eligibility.Country, review)),
            new(
                EligibilityRuleType.MinimumServiceMonths,
                eligibility.MinimumServiceMonths.Value?.ToString(CultureInfo.InvariantCulture),
                Assess(eligibility.MinimumServiceMonths, review)),
            new(EligibilityRuleType.MinimumGrade, eligibility.MinimumGrade.Value, Assess(eligibility.MinimumGrade, review)),
        ];

        return new ExtractedPolicyFacts(header, fields, coverageItems, rules);
    }

    private static CoverageItem ToCoverageItem(CoverageType type, ExtractedFact<CoverageTerms?> fact, ReviewPolicy review) =>
        new(type, fact.Value?.Covered, fact.Value?.Limit, fact.Value?.SessionLimit, Assess(fact, review));

    private static FactAssessment Assess<T>(ExtractedFact<T> fact, ReviewPolicy review)
    {
        Evidence? evidence = fact.Evidence is { } source
            ? new Evidence(source.PageStart, source.PageEnd, source.Quote)
            : null;

        return review.Assess(fact.Confidence, ToReviewReasons(fact.Issues), evidence);
    }

    private static ReviewReasons ToReviewReasons(IEnumerable<FactIssue> issues) =>
        issues.Aggregate(ReviewReasons.None, (reasons, issue) => reasons | issue switch
        {
            FactIssue.Ambiguous => ReviewReasons.Ambiguous,
            FactIssue.EvidenceMissing => ReviewReasons.EvidenceMissing,
            FactIssue.EvidenceNotFound => ReviewReasons.EvidenceNotFound,
            FactIssue.ValueNotInEvidence => ReviewReasons.ValueNotInEvidence,
            _ => throw new ArgumentOutOfRangeException(nameof(issues), issue, "Unknown fact issue."),
        });
}
