namespace BenefitsIntelligence.Domain.Policies;

[Flags]
public enum ReviewReasons
{
    None = 0,
    LowConfidence = 1,
    Ambiguous = 2,
    EvidenceMissing = 4,
    EvidenceNotFound = 8,
    ValueNotInEvidence = 16,
}
