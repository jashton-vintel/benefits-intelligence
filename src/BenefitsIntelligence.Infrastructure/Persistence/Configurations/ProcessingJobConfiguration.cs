using BenefitsIntelligence.Domain.Policies;
using BenefitsIntelligence.Domain.Processing;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenefitsIntelligence.Infrastructure.Persistence.Configurations;

internal sealed class ProcessingJobConfiguration : IEntityTypeConfiguration<ProcessingJob>
{
    public void Configure(EntityTypeBuilder<ProcessingJob> builder)
    {
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).ValueGeneratedNever();
        builder.Property(j => j.PolicyId);
        builder.Property(j => j.CorrelationId);
        builder.Property(j => j.Status);
        builder.Property(j => j.FailureCode).HasMaxLength(50);
        builder.Property(j => j.FailureMessage).HasMaxLength(2000);
        builder.Property(j => j.CreatedAt);
        builder.Property(j => j.UpdatedAt);
        builder.Ignore(j => j.IsFinished);

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasOne<BenefitPolicy>()
            .WithMany()
            .HasForeignKey(j => j.PolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(j => j.CorrelationId).IsUnique();
        builder.HasIndex(j => new { j.PolicyId, j.CreatedAt });
    }
}
