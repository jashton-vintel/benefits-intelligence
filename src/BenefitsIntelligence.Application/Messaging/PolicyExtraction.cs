namespace BenefitsIntelligence.Application.Messaging;

/// <remarks>
/// Monetary values arrive as JSON strings (for example "150.00") so no precision is lost in
/// transit; the web serializer defaults read them straight into <see cref="decimal"/>.
/// </remarks>
public sealed record PolicyExtraction(string? Provider, string? SchemeName, decimal? AnnualExcess);
