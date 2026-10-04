namespace BenefitsIntelligence.Domain.Policies;

/// <summary>
/// Decides which extracted facts a person must confirm. Problems found during extraction always
/// need review; otherwise a fact needs review when its confidence is below the threshold.
/// </summary>
public sealed class ReviewPolicy
{
    public const double DefaultConfidenceThreshold = 0.8;

    public ReviewPolicy(double confidenceThreshold = DefaultConfidenceThreshold)
    {
        if (double.IsNaN(confidenceThreshold) || confidenceThreshold is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(confidenceThreshold), confidenceThreshold, "The threshold must be between 0 and 1.");
        }

        ConfidenceThreshold = confidenceThreshold;
    }

    public double ConfidenceThreshold { get; }

    public FactAssessment Assess(double confidence, ReviewReasons reported, Evidence? evidence)
    {
        ReviewReasons reasons = confidence < ConfidenceThreshold
            ? reported | ReviewReasons.LowConfidence
            : reported;

        return new FactAssessment(confidence, reasons, evidence);
    }
}
