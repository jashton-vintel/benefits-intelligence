namespace BenefitsIntelligence.Application.Questions;

public interface IPolicyQuestionAnswerer
{
    /// <summary>Answers from the supplied pages, or returns null when the service cannot answer at all.</summary>
    Task<PolicyAnswer?> AnswerAsync(PolicyQuestionRequest request, CancellationToken cancellationToken);
}
