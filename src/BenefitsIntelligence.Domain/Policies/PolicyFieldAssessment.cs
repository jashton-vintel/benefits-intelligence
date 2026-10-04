namespace BenefitsIntelligence.Domain.Policies;

public sealed class PolicyFieldAssessment
{
    public PolicyFieldAssessment(PolicyField field, FactAssessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        Id = Guid.NewGuid();
        Field = field;
        Assessment = assessment;
    }

    private PolicyFieldAssessment()
    {
        Assessment = null!;
    }

    public Guid Id { get; private set; }

    public PolicyField Field { get; private set; }

    public FactAssessment Assessment { get; private set; }
}
