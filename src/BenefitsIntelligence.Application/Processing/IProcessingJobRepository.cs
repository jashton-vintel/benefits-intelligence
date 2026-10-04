using BenefitsIntelligence.Domain.Policies;
using BenefitsIntelligence.Domain.Processing;

namespace BenefitsIntelligence.Application.Processing;

public interface IProcessingJobRepository
{
    Task<ProcessingJob?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken);

    Task<BenefitPolicy?> FindPolicyAsync(Guid policyId, CancellationToken cancellationToken);

    Task<PolicyDocument?> FindDocumentAsync(Guid documentId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
