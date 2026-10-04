namespace BenefitsIntelligence.Application.Messaging;

public sealed record ExtractedCoverage(
    ExtractedFact<CoverageTerms?> Inpatient,
    ExtractedFact<CoverageTerms?> Outpatient,
    ExtractedFact<CoverageTerms?> Diagnostics,
    ExtractedFact<CoverageTerms?> Physiotherapy,
    ExtractedFact<CoverageTerms?> MentalHealth,
    ExtractedFact<CoverageTerms?> Cancer);
