using BenefitsIntelligence.Domain.Policies;

namespace BenefitsIntelligence.Application.Policies;

public sealed record UploadPolicyCommand(
    Guid OrganisationId,
    string Name,
    BenefitType BenefitType,
    string FileName,
    Stream Content,
    long SizeBytes);
