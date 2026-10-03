using BenefitsIntelligence.Domain.Organisations;
using BenefitsIntelligence.Domain.Policies;
using BenefitsIntelligence.Domain.Processing;

using Microsoft.EntityFrameworkCore;

namespace BenefitsIntelligence.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Organisation> Organisations => Set<Organisation>();

    public DbSet<PolicyDocument> PolicyDocuments => Set<PolicyDocument>();

    public DbSet<BenefitPolicy> BenefitPolicies => Set<BenefitPolicy>();

    public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
