namespace BenefitsIntelligence.Application.Questions;

public sealed record QuestionResult(QuestionStatus Status, PolicyAnswer? Answer = null);
