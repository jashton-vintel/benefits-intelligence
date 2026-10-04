using BenefitsIntelligence.Domain.Organisations;
using BenefitsIntelligence.Domain.Policies;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenefitsIntelligence.Infrastructure.Persistence.Configurations;

internal sealed class BenefitPolicyConfiguration : IEntityTypeConfiguration<BenefitPolicy>
{
    public const string PolicyIdColumn = "PolicyId";

    public void Configure(EntityTypeBuilder<BenefitPolicy> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.OrganisationId);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.BenefitType);
        builder.Property(p => p.DocumentId);
        builder.Property(p => p.CreatedAt);
        builder.Property(p => p.Provider).HasMaxLength(200);
        builder.Property(p => p.SchemeName).HasMaxLength(200);
        builder.Property(p => p.AnnualPremium).HasPrecision(18, 2);
        builder.Property(p => p.AnnualExcess).HasPrecision(18, 2);
        builder.Property(p => p.EffectiveDate);
        builder.Property(p => p.RenewalDate);
        builder.Property(p => p.DependantsAllowed);
        builder.Property(p => p.DependantsIncluded);
        builder.Property(p => p.NeedsReview);
        builder.Ignore(p => p.HasExtraction);

        builder.HasOne<Organisation>()
            .WithMany()
            .HasForeignKey(p => p.OrganisationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PolicyDocument>()
            .WithOne()
            .HasForeignKey<BenefitPolicy>(p => p.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.FieldAssessments).WithOne().HasForeignKey(PolicyIdColumn).IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.CoverageItems).WithOne().HasForeignKey(PolicyIdColumn).IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.EligibilityRules).WithOne().HasForeignKey(PolicyIdColumn).IsRequired().OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.OrganisationId, p.CreatedAt });
    }
}
