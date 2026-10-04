namespace BenefitsIntelligence.Application.Messaging;

public sealed record ExtractedEligibility(
    ExtractedFact<string?> EmploymentType,
    ExtractedFact<string?> Country,
    ExtractedFact<int?> MinimumServiceMonths,
    ExtractedFact<string?> MinimumGrade);
