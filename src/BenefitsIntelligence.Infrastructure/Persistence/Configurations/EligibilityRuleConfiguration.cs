using BenefitsIntelligence.Domain.Policies;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenefitsIntelligence.Infrastructure.Persistence.Configurations;

internal sealed class EligibilityRuleConfiguration : IEntityTypeConfiguration<EligibilityRule>
{
    public void Configure(EntityTypeBuilder<EligibilityRule> builder)
    {
        builder.ToTable("EligibilityRules");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Type);
        builder.Property(r => r.Value).HasMaxLength(EligibilityRule.MaxValueLength);
        builder.MapAssessment(r => r.Assessment);

        builder.HasIndex(BenefitPolicyConfiguration.PolicyIdColumn, nameof(EligibilityRule.Type)).IsUnique();
    }
}
