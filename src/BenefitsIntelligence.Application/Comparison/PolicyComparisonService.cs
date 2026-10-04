using BenefitsIntelligence.Application.Policies;
using BenefitsIntelligence.Domain.Comparison;
using BenefitsIntelligence.Domain.Policies;

namespace BenefitsIntelligence.Application.Comparison;

public sealed class PolicyComparisonService(IPolicyRepository policies, IComparisonSummariser summariser)
{
    public async Task<ComparisonResult> CompareAsync(
        Guid organisationId,
        Guid currentId,
        Guid proposedId,
        bool includeSummary,
        CancellationToken cancellationToken)
    {
        BenefitPolicy? current = await policies.FindWithExtractionAsync(organisationId, currentId, cancellationToken);
        if (current is null)
        {
            return ComparisonResult.NotFound(currentId);
        }

        BenefitPolicy? proposed = await policies.FindWithExtractionAsync(organisationId, proposedId, cancellationToken);
        if (proposed is null)
        {
            return ComparisonResult.NotFound(proposedId);
        }

        if (!current.HasExtraction)
        {
            return ComparisonResult.NotReady(current.Id);
        }

        if (!proposed.HasExtraction)
        {
            return ComparisonResult.NotReady(proposed.Id);
        }

        PolicyComparison comparison = PolicyComparer.Compare(current, proposed);
        ComparisonReport report = new(Reference(current), Reference(proposed), comparison.Differences);

        if (includeSummary)
        {
            ComparisonSummaryRequest request = new(report.Current, report.Proposed, report.Differences);
            report = report with { Summary = await summariser.SummariseAsync(request, cancellationToken) };
        }

        return new ComparisonResult(ComparisonStatus.Compared, report);
    }

    private static PolicyReference Reference(BenefitPolicy policy) => new(policy.Id, policy.Name, policy.Provider, policy.SchemeName);
}
