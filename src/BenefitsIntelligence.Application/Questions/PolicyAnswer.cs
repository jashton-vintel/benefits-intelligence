namespace BenefitsIntelligence.Application.Questions;

/// <param name="Supported">False when the policy does not answer the question; the answer is then a refusal.</param>
public sealed record PolicyAnswer(string Answer, bool Supported, IReadOnlyList<AnswerCitation> Citations);
