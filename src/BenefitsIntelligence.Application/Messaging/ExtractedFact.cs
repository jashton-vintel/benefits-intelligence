namespace BenefitsIntelligence.Application.Messaging;

public sealed record ExtractedFact<T>(T Value, double Confidence, FactEvidence? Evidence, IReadOnlyList<FactIssue> Issues);
