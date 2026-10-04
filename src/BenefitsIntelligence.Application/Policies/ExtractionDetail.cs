using BenefitsIntelligence.Domain.Policies;

namespace BenefitsIntelligence.Application.Policies;

public sealed record ExtractionDetail(
    bool NeedsReview,
    FactDetail<string?> Provider,
    FactDetail<string?> SchemeName,
    FactDetail<decimal?> AnnualPremium,
    FactDetail<decimal?> AnnualExcess,
    FactDetail<DateOnly?> EffectiveDate,
    FactDetail<DateOnly?> RenewalDate,
    FactDetail<bool?> DependantsAllowed,
    FactDetail<bool?> DependantsIncluded,
    IReadOnlyList<CoverageDetail> Coverage,
    IReadOnlyList<EligibilityDetail> Eligibility)
{
    public static ExtractionDetail? From(BenefitPolicy policy)
    {
        if (!policy.HasExtraction)
        {
            return null;
        }

        Dictionary<PolicyField, AssessmentDetail> fields = policy.FieldAssessments
            .ToDictionary(a => a.Field, a => AssessmentDetail.From(a.Assessment));

        return new ExtractionDetail(
            policy.NeedsReview,
            new(policy.Provider, fields[PolicyField.Provider]),
            new(policy.SchemeName, fields[PolicyField.SchemeName]),
            new(policy.AnnualPremium, fields[PolicyField.AnnualPremium]),
            new(policy.AnnualExcess, fields[PolicyField.AnnualExcess]),
            new(policy.EffectiveDate, fields[PolicyField.EffectiveDate]),
            new(policy.RenewalDate, fields[PolicyField.RenewalDate]),
            new(policy.DependantsAllowed, fields[PolicyField.DependantsAllowed]),
            new(policy.DependantsIncluded, fields[PolicyField.DependantsIncluded]),
            policy.CoverageItems
                .OrderBy(c => c.Type)
                .Select(c => new CoverageDetail(c.Type, c.Covered, c.Limit, c.SessionLimit, AssessmentDetail.From(c.Assessment)))
                .ToList(),
            policy.EligibilityRules
                .OrderBy(r => r.Type)
                .Select(r => new EligibilityDetail(r.Type, r.Value, AssessmentDetail.From(r.Assessment)))
                .ToList());
    }
}
