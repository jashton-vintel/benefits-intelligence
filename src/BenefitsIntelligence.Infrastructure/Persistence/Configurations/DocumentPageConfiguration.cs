using BenefitsIntelligence.Domain.Policies;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenefitsIntelligence.Infrastructure.Persistence.Configurations;

internal sealed class DocumentPageConfiguration : IEntityTypeConfiguration<DocumentPage>
{
    public const string DocumentIdColumn = "DocumentId";

    public void Configure(EntityTypeBuilder<DocumentPage> builder)
    {
        builder.ToTable("PolicyDocumentPages");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.PageNumber);
        builder.Property(p => p.Text).IsRequired();
        builder.Property<Guid>(DocumentIdColumn);

        builder.HasIndex(DocumentIdColumn, nameof(DocumentPage.PageNumber)).IsUnique();
    }
}
