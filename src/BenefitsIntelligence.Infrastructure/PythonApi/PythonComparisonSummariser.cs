using BenefitsIntelligence.Application.Comparison;

namespace BenefitsIntelligence.Infrastructure.PythonApi;

internal sealed class PythonComparisonSummariser(PythonApiClient api) : IComparisonSummariser
{
    public async Task<string?> SummariseAsync(ComparisonSummaryRequest request, CancellationToken cancellationToken)
    {
        ComparisonSummaryResponse? response = await api.PostAsync<ComparisonSummaryRequest, ComparisonSummaryResponse>(
            "summaries/comparison",
            request,
            cancellationToken);

        return string.IsNullOrWhiteSpace(response?.Summary) ? null : response.Summary;
    }
}
