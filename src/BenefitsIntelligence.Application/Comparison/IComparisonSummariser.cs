namespace BenefitsIntelligence.Application.Comparison;

public interface IComparisonSummariser
{
    /// <summary>
    /// Writes a factual summary of the differences, or returns null when none can be produced.
    /// A summary is optional, so being unable to write one is not an error.
    /// </summary>
    Task<string?> SummariseAsync(ComparisonSummaryRequest request, CancellationToken cancellationToken);
}
