using BenefitsIntelligence.Domain.Policies;

namespace BenefitsIntelligence.Domain.Tests.Policies;

public class ReviewPolicyTests
{
    private static readonly Evidence Evidence = new(3, 3, "An excess of £150 applies");

    private readonly ReviewPolicy _policy = new(confidenceThreshold: 0.8);

    [Theory]
    [InlineData(0.79, true)]
    [InlineData(0.8, false)]
    [InlineData(1.0, false)]
    public void FactBelowTheThresholdNeedsReview(double confidence, bool needsReview)
    {
        FactAssessment assessment = _policy.Assess(confidence, ReviewReasons.None, Evidence);

        Assert.Equal(needsReview, assessment.NeedsReview);
        Assert.Equal(needsReview, assessment.ReviewReasons.HasFlag(ReviewReasons.LowConfidence));
    }

    [Fact]
    public void ReportedProblemsNeedReviewWhateverTheConfidence()
    {
        FactAssessment assessment = _policy.Assess(1.0, ReviewReasons.EvidenceNotFound, evidence: null);

        Assert.True(assessment.NeedsReview);
        Assert.Equal(ReviewReasons.EvidenceNotFound, assessment.ReviewReasons);
    }

    [Fact]
    public void LowConfidenceIsAddedToReportedProblems()
    {
        FactAssessment assessment = _policy.Assess(0.4, ReviewReasons.Ambiguous, Evidence);

        Assert.Equal(ReviewReasons.Ambiguous | ReviewReasons.LowConfidence, assessment.ReviewReasons);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void ThresholdMustBeBetweenZeroAndOne(double threshold)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReviewPolicy(threshold));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void ConfidenceMustBeBetweenZeroAndOne(double confidence)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _policy.Assess(confidence, ReviewReasons.None, Evidence));
    }

    [Theory]
    [InlineData(0, 1, "quote")]
    [InlineData(3, 2, "quote")]
    [InlineData(1, 1, " ")]
    public void EvidenceRequiresPagesInOrderAndAQuote(int pageStart, int pageEnd, string quote)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Evidence(pageStart, pageEnd, quote));
    }
}
