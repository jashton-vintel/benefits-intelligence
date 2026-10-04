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

    public async Task<PolicyDetail?> FindAsync(Guid organisationId, Guid policyId, CancellationToken cancellationToken)
    {
        IQueryable<BenefitPolicy> policies = db.BenefitPolicies
            .Where(p => p.OrganisationId == organisationId && p.Id == policyId);

        PolicySummary? summary = await ToSummaries(policies).SingleOrDefaultAsync(cancellationToken);
        if (summary is null)
        {
            return null;
        }

        BenefitPolicy policy = await policies
            .AsNoTracking()
            .Include(p => p.FieldAssessments)
            .Include(p => p.CoverageItems)
            .Include(p => p.EligibilityRules)
            .AsSplitQuery()
            .SingleAsync(cancellationToken);

        return new PolicyDetail(
            summary.Id,
            summary.Name,
            summary.BenefitType,
            summary.FileName,
            summary.CreatedAt,
            summary.Status,
            summary.FailureCode,
            summary.FailureMessage,
            summary.StatusUpdatedAt,
            ExtractionDetail.From(policy));
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
                row.policy.NeedsReview,
                row.job.Status,
                row.job.FailureCode,
                row.job.FailureMessage,
                row.job.UpdatedAt));
    }
}
