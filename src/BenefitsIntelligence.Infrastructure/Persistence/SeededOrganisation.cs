namespace BenefitsIntelligence.Infrastructure.Persistence;

/// <summary>
/// Single tenant used until authentication supplies the organisation from the caller's claims.
/// </summary>
public static class SeededOrganisation
{
    public static readonly Guid Id = new("11111111-1111-1111-1111-111111111111");

    public const string Name = "Demo Organisation";
}
