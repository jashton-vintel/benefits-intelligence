using BenefitsIntelligence.Domain.Policies;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenefitsIntelligence.Infrastructure.Persistence.Configurations;

internal sealed class CoverageItemConfiguration : IEntityTypeConfiguration<CoverageItem>
{
    public void Configure(EntityTypeBuilder<CoverageItem> builder)
    {
        builder.ToTable("CoverageItems");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Type);
        builder.Property(c => c.Covered);
        builder.Property(c => c.Limit).HasMaxLength(CoverageItem.MaxLimitLength);
        builder.Property(c => c.SessionLimit);
        builder.MapAssessment(c => c.Assessment);

        builder.HasIndex(BenefitPolicyConfiguration.PolicyIdColumn, nameof(CoverageItem.Type)).IsUnique();
    }
}
