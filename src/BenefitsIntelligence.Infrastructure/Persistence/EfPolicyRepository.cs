using BenefitsIntelligence.Application.Persistence;
using BenefitsIntelligence.Application.Policies;
using BenefitsIntelligence.Domain.Policies;
using BenefitsIntelligence.Domain.Processing;

using Microsoft.EntityFrameworkCore;

namespace BenefitsIntelligence.Infrastructure.Persistence;

internal sealed class EfPolicyRepository(AppDbContext db) : IPolicyRepository
{
    public void Add(PolicyDocument document, BenefitPolicy policy, ProcessingJob job)
    {
        db.PolicyDocuments.Add(document);
        db.BenefitPolicies.Add(policy);
        db.ProcessingJobs.Add(job);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException("The record was modified by another operation.", exception);
        }
    }
}
