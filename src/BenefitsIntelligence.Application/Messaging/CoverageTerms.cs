namespace BenefitsIntelligence.Application.Messaging;

public sealed record CoverageTerms(bool Covered, string? Limit, int? SessionLimit);
