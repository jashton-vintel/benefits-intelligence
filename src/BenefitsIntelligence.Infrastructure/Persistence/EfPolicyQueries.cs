using BenefitsIntelligence.Application.Policies;
using BenefitsIntelligence.Domain.Policies;

using Microsoft.EntityFrameworkCore;

namespace BenefitsIntelligence.Infrastructure.Persistence;

internal sealed class EfPolicyQueries(AppDbContext db) : IPolicyQueries
{
    public async Task<IReadOnlyList<PolicySummary>> ListAsync(Guid organisationId, CancellationToken cancellationToken)
    {
        IQueryable<BenefitPolicy> policies = db.BenefitPolicies
            .Where(p => p.OrganisationId == organisationId);

        return await ToSummaries(policies).ToListAsync(cancellationToken);
    }

    public async Task<PolicySummary?> FindAsync(Guid organisationId, Guid policyId, CancellationToken cancellationToken)
    {
        IQueryable<BenefitPolicy> policies = db.BenefitPolicies
            .Where(p => p.OrganisationId == organisationId && p.Id == policyId);

        return await ToSummaries(policies).SingleOrDefaultAsync(cancellationToken);
    }

    private IQueryable<PolicySummary> ToSummaries(IQueryable<BenefitPolicy> policies)
    {
        return policies
            .AsNoTracking()
            .Join(db.PolicyDocuments, p => p.DocumentId, d => d.Id, (policy, document) => new { policy, document })
            .SelectMany(
                row => db.ProcessingJobs
                    .Where(j => j.PolicyId == row.policy.Id)
                    .OrderByDescending(j => j.CreatedAt)
                    .Take(1),
                (row, job) => new { row.policy, row.document, job })
            .OrderByDescending(row => row.policy.CreatedAt)
            .Select(row => new PolicySummary(
                row.policy.Id,
                row.policy.Name,
                row.policy.BenefitType,
                row.document.FileName,
                row.policy.CreatedAt,
                row.policy.Provider,
                row.policy.SchemeName,
                row.policy.AnnualExcess,
                row.job.Status,
                row.job.FailureCode,
                row.job.FailureMessage,
                row.job.UpdatedAt));
    }
}
