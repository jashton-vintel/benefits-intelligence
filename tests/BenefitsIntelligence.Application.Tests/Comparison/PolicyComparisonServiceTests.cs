using BenefitsIntelligence.Application.Comparison;
using BenefitsIntelligence.Application.Tests.Fakes;
using BenefitsIntelligence.Domain.Comparison;
using BenefitsIntelligence.Domain.Policies;

namespace BenefitsIntelligence.Application.Tests.Comparison;

public class PolicyComparisonServiceTests
{
    private static readonly Guid OrganisationId = Guid.NewGuid();
    private static readonly FactAssessment Confirmed = new(0.95, ReviewReasons.None, new Evidence(1, 1, "quoted text"));

    private readonly FakePolicyRepository _policies = new(new CallLog());
    private readonly FakeComparisonSummariser _summariser = new();
    private readonly PolicyComparisonService _service;

    public PolicyComparisonServiceTests()
    {
        _service = new PolicyComparisonService(_policies, _summariser);
    }

    [Fact]
    public async Task ComparesTwoExtractedPolicies()
    {
        BenefitPolicy current = Extracted("Current", excess: 100m);
        BenefitPolicy proposed = Extracted("Proposed", excess: 150m);

        ComparisonResult result = await _service.CompareAsync(OrganisationId, current.Id, proposed.Id, includeSummary: false, CancellationToken.None);

        Assert.Equal(ComparisonStatus.Compared, result.Status);
        Assert.Equal(new PolicyReference(current.Id, "Current", "Atlas Healthcare", "Corporate Plus"), result.Report!.Current);
        Assert.Equal(Change.Increased, result.Report.Differences.Single(d => d.Field == "annual_excess").Change);
        Assert.Null(result.Report.Summary);
        Assert.Empty(_summariser.Requests);
    }

    [Fact]
    public async Task SummaryIsWrittenFromTheCalculatedDifferencesWhenRequested()
    {
        BenefitPolicy current = Extracted("Current", excess: 100m);
        BenefitPolicy proposed = Extracted("Proposed", excess: 150m);

        ComparisonResult result = await _service.CompareAsync(OrganisationId, current.Id, proposed.Id, includeSummary: true, CancellationToken.None);

        Assert.Equal("The annual excess increases from £100 to £150.", result.Report!.Summary);
        ComparisonSummaryRequest request = Assert.Single(_summariser.Requests);
        Assert.Equal(result.Report.Differences, request.Differences);
        Assert.Equal(result.Report.Proposed, request.Proposed);
    }

    [Fact]
    public async Task ComparisonIsReturnedEvenWhenNoSummaryCanBeWritten()
    {
        _summariser.Summary = null;
        BenefitPolicy current = Extracted("Current", excess: 100m);
        BenefitPolicy proposed = Extracted("Proposed", excess: 150m);

        ComparisonResult result = await _service.CompareAsync(OrganisationId, current.Id, proposed.Id, includeSummary: true, CancellationToken.None);

        Assert.Equal(ComparisonStatus.Compared, result.Status);
        Assert.NotEmpty(result.Report!.Differences);
        Assert.Null(result.Report.Summary);
    }

    [Fact]
    public async Task MissingPolicyIsNotFound()
    {
        BenefitPolicy current = Extracted("Current", excess: 100m);
        Guid missing = Guid.NewGuid();

        ComparisonResult result = await _service.CompareAsync(OrganisationId, current.Id, missing, includeSummary: false, CancellationToken.None);

        Assert.Equal(ComparisonResult.NotFound(missing), result);
    }

    [Fact]
    public async Task AnotherOrganisationsPolicyIsNotFound()
    {
        BenefitPolicy current = Extracted("Current", excess: 100m);
        BenefitPolicy otherTenants = Extracted("Proposed", excess: 150m, organisationId: Guid.NewGuid());

        ComparisonResult result = await _service.CompareAsync(OrganisationId, current.Id, otherTenants.Id, includeSummary: false, CancellationToken.None);

        Assert.Equal(ComparisonStatus.PolicyNotFound, result.Status);
        Assert.Null(result.Report);
    }

    [Fact]
    public async Task PolicyStillProcessingIsNotReady()
    {
        BenefitPolicy current = Extracted("Current", excess: 100m);
        BenefitPolicy processing = Policy("Proposed", OrganisationId);

        ComparisonResult result = await _service.CompareAsync(OrganisationId, current.Id, processing.Id, includeSummary: false, CancellationToken.None);

        Assert.Equal(ComparisonResult.NotReady(processing.Id), result);
    }

    private BenefitPolicy Extracted(string name, decimal excess, Guid? organisationId = null)
    {
        BenefitPolicy policy = Policy(name, organisationId ?? OrganisationId);
        policy.RecordExtraction(new ExtractedPolicyFacts(
            new PolicyHeader("Atlas Healthcare", "Corporate Plus", 120000m, excess, null, null, true, true),
            Enum.GetValues<PolicyField>().Select(field => new PolicyFieldAssessment(field, Confirmed)).ToList(),
            Enum.GetValues<CoverageType>().Select(type => new CoverageItem(type, true, "Paid in full", null, Confirmed)).ToList(),
            Enum.GetValues<EligibilityRuleType>().Select(type => new EligibilityRule(type, "Permanent", Confirmed)).ToList()));
        return policy;
    }

    private BenefitPolicy Policy(string name, Guid organisationId)
    {
        BenefitPolicy policy = new(Guid.NewGuid(), organisationId, name, BenefitType.PrivateMedicalInsurance, Guid.NewGuid(), DateTimeOffset.UtcNow);
        _policies.Existing.Add(policy);
        return policy;
    }
}
