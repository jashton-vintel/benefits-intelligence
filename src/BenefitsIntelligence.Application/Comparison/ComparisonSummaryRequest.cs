using BenefitsIntelligence.Domain.Comparison;

namespace BenefitsIntelligence.Application.Comparison;

/// <summary>What the summary is written from: the calculated differences, never the documents.</summary>
public sealed record ComparisonSummaryRequest(
    PolicyReference Current,
    PolicyReference Proposed,
    IReadOnlyList<FieldDifference> Differences);
