namespace BenefitsIntelligence.Application.Messaging;

/// <remarks>
/// Monetary values arrive as JSON strings (for example "150.00") so no precision is lost in
/// transit; the web serializer defaults read them straight into <see cref="decimal"/>.
/// </remarks>
public sealed record PolicyExtraction(
    string SchemaVersion,
    string BenefitType,
    ExtractedFact<string?> Provider,
    ExtractedFact<string?> SchemeName,
    ExtractedFact<decimal?> AnnualPremium,
    ExtractedFact<decimal?> AnnualExcess,
    ExtractedFact<DateOnly?> EffectiveDate,
    ExtractedFact<DateOnly?> RenewalDate,
    ExtractedFact<bool?> DependantsAllowed,
    ExtractedFact<bool?> DependantsIncluded,
    ExtractedCoverage Coverage,
    ExtractedEligibility Eligibility)
{
    public const string SupportedSchemaVersion = "1.0";
}
