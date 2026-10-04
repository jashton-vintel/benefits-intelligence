using BenefitsIntelligence.Application.Persistence;
using BenefitsIntelligence.Application.Processing;
using BenefitsIntelligence.Domain.Policies;
using BenefitsIntelligence.Domain.Processing;

using Microsoft.EntityFrameworkCore;

namespace BenefitsIntelligence.Infrastructure.Persistence;

internal sealed class EfProcessingJobRepository(AppDbContext db) : IProcessingJobRepository
{
    public Task<ProcessingJob?> FindByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken) =>
        db.ProcessingJobs.SingleOrDefaultAsync(j => j.CorrelationId == correlationId, cancellationToken);

    public Task<BenefitPolicy?> FindPolicyAsync(Guid policyId, CancellationToken cancellationToken) =>
        db.BenefitPolicies.SingleOrDefaultAsync(p => p.Id == policyId, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException("The processing job was modified by another operation.", exception);
        }
    }
}
