using BenefitsIntelligence.Domain.Policies;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenefitsIntelligence.Infrastructure.Persistence.Configurations;

internal sealed class PolicyFieldAssessmentConfiguration : IEntityTypeConfiguration<PolicyFieldAssessment>
{
    public void Configure(EntityTypeBuilder<PolicyFieldAssessment> builder)
    {
        builder.ToTable("PolicyFieldAssessments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Field);
        builder.MapAssessment(a => a.Assessment);

        builder.HasIndex(BenefitPolicyConfiguration.PolicyIdColumn, nameof(PolicyFieldAssessment.Field)).IsUnique();
    }
}
