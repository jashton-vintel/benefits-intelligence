namespace BenefitsIntelligence.Domain.Policies;

public sealed record PolicyHeader(
    string? Provider,
    string? SchemeName,
    decimal? AnnualPremium,
    decimal? AnnualExcess,
    DateOnly? EffectiveDate,
    DateOnly? RenewalDate,
    bool? DependantsAllowed,
    bool? DependantsIncluded);
