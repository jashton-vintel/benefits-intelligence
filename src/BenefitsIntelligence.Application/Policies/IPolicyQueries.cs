namespace BenefitsIntelligence.Application.Policies;

public interface IPolicyQueries
{
    Task<IReadOnlyList<PolicySummary>> ListAsync(Guid organisationId, CancellationToken cancellationToken);

    Task<PolicyDetail?> FindAsync(Guid organisationId, Guid policyId, CancellationToken cancellationToken);
}
