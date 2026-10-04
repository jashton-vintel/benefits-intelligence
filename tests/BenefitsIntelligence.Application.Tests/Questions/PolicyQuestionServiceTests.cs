using BenefitsIntelligence.Application.Questions;
using BenefitsIntelligence.Application.Tests.Fakes;
using BenefitsIntelligence.Domain.Policies;

namespace BenefitsIntelligence.Application.Tests.Questions;

public class PolicyQuestionServiceTests
{
    private static readonly Guid OrganisationId = Guid.NewGuid();
    private static readonly FactAssessment Confirmed = new(0.95, ReviewReasons.None, new Evidence(1, 1, "quoted text"));

    private readonly FakePolicyRepository _policies = new(new CallLog());
    private readonly FakeQuestionAnswerer _answerer = new();
    private readonly PolicyQuestionService _service;

    public PolicyQuestionServiceTests()
    {
        _service = new PolicyQuestionService(_policies, _answerer);
    }

    [Fact]
    public async Task AnswersFromThePolicysOwnPages()
    {
        BenefitPolicy policy = Policy(extracted: true, pages: ["First page", "Physiotherapy is covered."]);

        QuestionResult result = await _service.AskAsync(OrganisationId, policy.Id, "  Is physiotherapy covered?  ", CancellationToken.None);

        Assert.Equal(QuestionStatus.Answered, result.Status);
        Assert.Equal(_answerer.Answer, result.Answer);
        PolicyQuestionRequest request = Assert.Single(_answerer.Requests);
        Assert.Equal("Is physiotherapy covered?", request.Question);
        Assert.Equal("Proposed policy", request.PolicyName);
        Assert.Equal([1, 2], request.Pages.Select(p => p.PageNumber));
    }

    [Fact]
    public async Task AnotherOrganisationsPolicyIsNotFoundAndItsTextIsNeverSent()
    {
        BenefitPolicy policy = Policy(extracted: true, pages: ["Text"], organisationId: Guid.NewGuid());

        QuestionResult result = await _service.AskAsync(OrganisationId, policy.Id, "Is physiotherapy covered?", CancellationToken.None);

        Assert.Equal(QuestionStatus.PolicyNotFound, result.Status);
        Assert.Empty(_answerer.Requests);
    }

    [Fact]
    public async Task PolicyStillProcessingIsNotReady()
    {
        BenefitPolicy policy = Policy(extracted: false, pages: []);

        QuestionResult result = await _service.AskAsync(OrganisationId, policy.Id, "Is physiotherapy covered?", CancellationToken.None);

        Assert.Equal(QuestionStatus.PolicyNotReady, result.Status);
    }

    [Fact]
    public async Task PolicyProcessedWithoutStoredTextCannotBeAsked()
    {
        BenefitPolicy policy = Policy(extracted: true, pages: []);

        QuestionResult result = await _service.AskAsync(OrganisationId, policy.Id, "Is physiotherapy covered?", CancellationToken.None);

        Assert.Equal(QuestionStatus.TextUnavailable, result.Status);
        Assert.Empty(_answerer.Requests);
    }

    [Fact]
    public async Task UnavailableAnsweringServiceIsReportedAsSuch()
    {
        _answerer.Answer = null;
        BenefitPolicy policy = Policy(extracted: true, pages: ["Text"]);

        QuestionResult result = await _service.AskAsync(OrganisationId, policy.Id, "Is physiotherapy covered?", CancellationToken.None);

        Assert.Equal(QuestionStatus.AnswersUnavailable, result.Status);
    }

    private BenefitPolicy Policy(bool extracted, string[] pages, Guid? organisationId = null)
    {
        Guid organisation = organisationId ?? OrganisationId;
        PolicyDocument document = new(Guid.NewGuid(), organisation, "policy.pdf", "documents/policy.pdf", "application/pdf", 1024, DateTimeOffset.UtcNow);
        if (pages.Length > 0)
        {
            document.RecordPages(pages.Select((text, index) => new DocumentPage(index + 1, text)));
        }

        BenefitPolicy policy = new(Guid.NewGuid(), organisation, "Proposed policy", BenefitType.PrivateMedicalInsurance, document.Id, DateTimeOffset.UtcNow);
        if (extracted)
        {
            policy.RecordExtraction(new ExtractedPolicyFacts(
                new PolicyHeader("NorthStar Health", "Essentials Select", 108000m, 150m, null, null, true, null),
                Enum.GetValues<PolicyField>().Select(field => new PolicyFieldAssessment(field, Confirmed)).ToList(),
                Enum.GetValues<CoverageType>().Select(type => new CoverageItem(type, true, "Full cover", null, Confirmed)).ToList(),
                Enum.GetValues<EligibilityRuleType>().Select(type => new EligibilityRule(type, "Permanent", Confirmed)).ToList()));
        }

        _policies.Documents.Add(document);
        _policies.Existing.Add(policy);
        return policy;
    }
}
