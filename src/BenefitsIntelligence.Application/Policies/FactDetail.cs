namespace BenefitsIntelligence.Application.Policies;

public sealed record FactDetail<T>(T Value, AssessmentDetail Assessment);
