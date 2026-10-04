using BenefitsIntelligence.Application.Questions;

namespace BenefitsIntelligence.Application.Tests.Fakes;

internal sealed class FakeQuestionAnswerer : IPolicyQuestionAnswerer
{
    public PolicyAnswer? Answer { get; set; } = new(
        "Yes, physiotherapy is covered for up to ten sessions.",
        Supported: true,
        [new AnswerCitation(4, 4, "Physiotherapy is covered for up to ten sessions")]);

    public List<PolicyQuestionRequest> Requests { get; } = [];

    public Task<PolicyAnswer?> AnswerAsync(PolicyQuestionRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(Answer);
    }
}
