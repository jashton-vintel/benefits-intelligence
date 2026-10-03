namespace BenefitsIntelligence.Application.Policies;

public interface IPolicyQueries
{
    Task<IReadOnlyList<PolicySummary>> ListAsync(Guid organisationId, CancellationToken cancellationToken);

    Task<PolicySummary?> FindAsync(Guid organisationId, Guid policyId, CancellationToken cancellationToken);
}
