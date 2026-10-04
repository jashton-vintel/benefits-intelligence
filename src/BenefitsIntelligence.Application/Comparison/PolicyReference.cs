namespace BenefitsIntelligence.Application.Comparison;

public sealed record PolicyReference(Guid Id, string Name, string? Provider, string? SchemeName);
