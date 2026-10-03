using BenefitsIntelligence.Domain.Organisations;
using BenefitsIntelligence.Domain.Policies;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenefitsIntelligence.Infrastructure.Persistence.Configurations;

internal sealed class BenefitPolicyConfiguration : IEntityTypeConfiguration<BenefitPolicy>
{
    public void Configure(EntityTypeBuilder<BenefitPolicy> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.OrganisationId);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.BenefitType);
        builder.Property(p => p.DocumentId);
        builder.Property(p => p.CreatedAt);

        builder.HasOne<Organisation>()
            .WithMany()
            .HasForeignKey(p => p.OrganisationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PolicyDocument>()
            .WithOne()
            .HasForeignKey<BenefitPolicy>(p => p.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.OrganisationId, p.CreatedAt });
    }
}
