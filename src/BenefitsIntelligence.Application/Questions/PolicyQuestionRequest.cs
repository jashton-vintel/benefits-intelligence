using BenefitsIntelligence.Application.Messaging;

namespace BenefitsIntelligence.Application.Questions;

/// <summary>A question with the text it must be answered from, so the answering service holds no data of its own.</summary>
public sealed record PolicyQuestionRequest(string Question, string PolicyName, IReadOnlyList<PageContent> Pages);
