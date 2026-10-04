using BenefitsIntelligence.Domain.Policies;

namespace BenefitsIntelligence.Application.Policies;

public sealed record AssessmentDetail(
    double Confidence,
    bool NeedsReview,
    IReadOnlyList<ReviewReasons> ReviewReasons,
    Evidence? Evidence)
{
    public static AssessmentDetail From(FactAssessment assessment) =>
        new(
            assessment.Confidence,
            assessment.NeedsReview,
            Enum.GetValues<ReviewReasons>()
                .Where(reason => reason != Domain.Policies.ReviewReasons.None && assessment.ReviewReasons.HasFlag(reason))
                .ToList(),
            assessment.Evidence);
}
