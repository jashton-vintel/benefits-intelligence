using BenefitsIntelligence.Application.Questions;

namespace BenefitsIntelligence.Infrastructure.PythonApi;

internal sealed class PythonQuestionAnswerer(PythonApiClient api) : IPolicyQuestionAnswerer
{
    public Task<PolicyAnswer?> AnswerAsync(PolicyQuestionRequest request, CancellationToken cancellationToken) =>
        api.PostAsync<PolicyQuestionRequest, PolicyAnswer>("answers", request, cancellationToken);
}
