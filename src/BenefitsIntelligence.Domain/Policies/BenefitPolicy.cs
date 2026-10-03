namespace BenefitsIntelligence.Domain.Policies;

public sealed class BenefitPolicy
{
    public BenefitPolicy(
        Guid id,
        Guid organisationId,
        string name,
        BenefitType benefitType,
        Guid documentId,
        DateTimeOffset createdAt)
    {
        Id = id;
        OrganisationId = organisationId;
        Name = name;
        BenefitType = benefitType;
        DocumentId = documentId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public Guid OrganisationId { get; }

    public string Name { get; private set; }

    public BenefitType BenefitType { get; }

    public Guid DocumentId { get; }

    public DateTimeOffset CreatedAt { get; }
}
