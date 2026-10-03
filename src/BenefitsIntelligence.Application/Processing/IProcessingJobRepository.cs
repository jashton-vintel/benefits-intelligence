using BenefitsIntelligence.Domain.Processing;

namespace BenefitsIntelligence.Application.Processing;

public interface IProcessingJobRepository
{
    Task<ProcessingJob?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
