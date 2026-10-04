namespace BenefitsIntelligence.Domain.Comparison;

/// <summary>
/// How one field differs between two policies. Values are invariant-culture strings (for example
/// "120000.00", "2027-04-01", "true") so a comparison can be shown or summarised without knowing
/// each field's type; <see cref="Kind"/> says how to read them.
/// </summary>
/// <param name="Field">Path of the field, matching the extraction schema, for example "coverage.physiotherapy.sessions".</param>
/// <param name="Delta">Proposed minus current, for money and counts where both sides have a value.</param>
/// <param name="NeedsReview">True when either value is flagged for review, so the difference is not yet reliable.</param>
public sealed record FieldDifference(
    string Field,
    ValueKind Kind,
    string? Current,
    string? Proposed,
    decimal? Delta,
    Change Change,
    bool NeedsReview);
