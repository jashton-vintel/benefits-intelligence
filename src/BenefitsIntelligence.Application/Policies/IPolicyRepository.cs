using BenefitsIntelligence.Domain.Policies;
using BenefitsIntelligence.Domain.Processing;

namespace BenefitsIntelligence.Application.Policies;

public interface IPolicyRepository
{
    void Add(PolicyDocument document, BenefitPolicy policy, ProcessingJob job);

    /// <summary>Loads a policy with everything extracted for it, without tracking changes.</summary>
    Task<BenefitPolicy?> FindWithExtractionAsync(Guid organisationId, Guid policyId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
