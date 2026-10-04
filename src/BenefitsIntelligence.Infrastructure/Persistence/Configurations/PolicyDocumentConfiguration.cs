using BenefitsIntelligence.Domain.Organisations;
using BenefitsIntelligence.Domain.Policies;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenefitsIntelligence.Infrastructure.Persistence.Configurations;

internal sealed class PolicyDocumentConfiguration : IEntityTypeConfiguration<PolicyDocument>
{
    public void Configure(EntityTypeBuilder<PolicyDocument> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.OrganisationId);
        builder.Property(d => d.FileName).HasMaxLength(255).IsRequired();
        builder.Property(d => d.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.SizeBytes);
        builder.Property(d => d.UploadedAt);

        builder.HasMany(d => d.Pages)
            .WithOne()
            .HasForeignKey(DocumentPageConfiguration.DocumentIdColumn)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Organisation>()
            .WithMany()
            .HasForeignKey(d => d.OrganisationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
