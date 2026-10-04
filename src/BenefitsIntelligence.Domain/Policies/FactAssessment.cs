namespace BenefitsIntelligence.Domain.Policies;

public sealed record FactAssessment
{
    public FactAssessment(double confidence, ReviewReasons reviewReasons, Evidence? evidence)
    {
        if (double.IsNaN(confidence) || confidence is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), confidence, "Confidence must be between 0 and 1.");
        }

        Confidence = confidence;
        ReviewReasons = reviewReasons;
        Evidence = evidence;
    }

    private FactAssessment()
    {
    }

    public double Confidence { get; }

    public ReviewReasons ReviewReasons { get; }

    public Evidence? Evidence { get; }

    public bool NeedsReview => ReviewReasons != ReviewReasons.None;
}
