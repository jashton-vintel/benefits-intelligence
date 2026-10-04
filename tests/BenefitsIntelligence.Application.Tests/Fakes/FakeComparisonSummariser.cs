using BenefitsIntelligence.Application.Comparison;

namespace BenefitsIntelligence.Application.Tests.Fakes;

internal sealed class FakeComparisonSummariser : IComparisonSummariser
{
    public string? Summary { get; set; } = "The annual excess increases from £100 to £150.";

    public List<ComparisonSummaryRequest> Requests { get; } = [];

    public Task<string?> SummariseAsync(ComparisonSummaryRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(Summary);
    }
}
