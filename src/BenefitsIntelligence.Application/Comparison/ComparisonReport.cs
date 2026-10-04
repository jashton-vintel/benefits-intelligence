using BenefitsIntelligence.Domain.Comparison;

namespace BenefitsIntelligence.Application.Comparison;

/// <param name="Summary">A written summary, when one was requested and could be produced.</param>
public sealed record ComparisonReport(
    PolicyReference Current,
    PolicyReference Proposed,
    IReadOnlyList<FieldDifference> Differences,
    string? Summary = null);
