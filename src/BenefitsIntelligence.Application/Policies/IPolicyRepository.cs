using BenefitsIntelligence.Domain.Policies;
using BenefitsIntelligence.Domain.Processing;

namespace BenefitsIntelligence.Application.Policies;

public interface IPolicyRepository
{
    void Add(PolicyDocument document, BenefitPolicy policy, ProcessingJob job);    
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
