using BenefitsIntelligence.Domain.Policies;

namespace BenefitsIntelligence.Domain.Tests.Policies;

public class BenefitPolicyTests
{
    [Fact]
    public void NewPolicyHasNoExtractedDetails()
    {
        BenefitPolicy policy = NewPolicy();

        Assert.Null(policy.Provider);
        Assert.Null(policy.SchemeName);
        Assert.Null(policy.AnnualExcess);
    }

    [Fact]
    public void RecordExtractionStoresTheExtractedDetails()
    {
        BenefitPolicy policy = NewPolicy();

        policy.RecordExtraction("Atlas Healthcare", "Corporate Plus", 100m);

        Assert.Equal("Atlas Healthcare", policy.Provider);
        Assert.Equal("Corporate Plus", policy.SchemeName);
        Assert.Equal(100m, policy.AnnualExcess);
    }

    [Fact]
    public void RecordExtractionAllowsValuesTheDocumentDidNotState()
    {
        BenefitPolicy policy = NewPolicy();
        policy.RecordExtraction("Atlas Healthcare", "Corporate Plus", 100m);

        policy.RecordExtraction("Atlas Healthcare", schemeName: null, annualExcess: null);

        Assert.Null(policy.SchemeName);
        Assert.Null(policy.AnnualExcess);
    }

    [Fact]
    public void RecordExtractionRejectsNegativeExcess()
    {
        BenefitPolicy policy = NewPolicy();

        Assert.Throws<ArgumentOutOfRangeException>(() => policy.RecordExtraction("Atlas Healthcare", "Corporate Plus", -1m));
        Assert.Null(policy.Provider);
    }

    private static BenefitPolicy NewPolicy() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Current policy", BenefitType.PrivateMedicalInsurance, Guid.NewGuid(), DateTimeOffset.UtcNow);
}
