using System.Linq.Expressions;

using BenefitsIntelligence.Domain.Policies;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenefitsIntelligence.Infrastructure.Persistence.Configurations;

internal static class FactAssessmentMapping
{
    public static void MapAssessment<TEntity>(this EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, FactAssessment?>> assessment)
        where TEntity : class
    {
        builder.ComplexProperty(assessment, a =>
        {
            a.Property(x => x.Confidence).HasColumnName("Confidence");
            a.Property(x => x.ReviewReasons).HasColumnName("ReviewReasons");
            a.Ignore(x => x.NeedsReview);

            a.ComplexProperty<Evidence>(x => x.Evidence!, e =>
            {
                e.Property(x => x.PageStart).HasColumnName("EvidencePageStart");
                e.Property(x => x.PageEnd).HasColumnName("EvidencePageEnd");
                e.Property(x => x.Quote).HasColumnName("EvidenceQuote").HasMaxLength(Evidence.MaxQuoteLength);
            });
        });
    }
}
