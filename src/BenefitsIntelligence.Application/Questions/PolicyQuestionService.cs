using BenefitsIntelligence.Application.Messaging;
using BenefitsIntelligence.Application.Policies;
using BenefitsIntelligence.Domain.Policies;

namespace BenefitsIntelligence.Application.Questions;

public sealed class PolicyQuestionService(IPolicyRepository policies, IPolicyQuestionAnswerer answerer)
{
    public const int MaxQuestionLength = 500;

    public async Task<QuestionResult> AskAsync(Guid organisationId, Guid policyId, string question, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);

        // The organisation check happens here, before any text leaves the API.
        BenefitPolicy? policy = await policies.FindWithExtractionAsync(organisationId, policyId, cancellationToken);
        if (policy is null)
        {
            return new QuestionResult(QuestionStatus.PolicyNotFound);
        }

        if (!policy.HasExtraction)
        {
            return new QuestionResult(QuestionStatus.PolicyNotReady);
        }

        IReadOnlyList<DocumentPage> pages = await policies.FindPagesAsync(organisationId, policy.DocumentId, cancellationToken);
        if (pages.Count == 0)
        {
            return new QuestionResult(QuestionStatus.TextUnavailable);
        }

        PolicyQuestionRequest request = new(
            question.Trim(),
            policy.Name,
            pages.Select(page => new PageContent(page.PageNumber, page.Text)).ToList());

        PolicyAnswer? answer = await answerer.AnswerAsync(request, cancellationToken);

        return answer is null
            ? new QuestionResult(QuestionStatus.AnswersUnavailable)
            : new QuestionResult(QuestionStatus.Answered, answer);
    }
}
