namespace BenefitsIntelligence.Application.Questions;

public enum QuestionStatus
{
    Answered,
    PolicyNotFound,
    PolicyNotReady,

    /// <summary>The policy was processed before its text was kept, so there is nothing to answer from.</summary>
    TextUnavailable,
    AnswersUnavailable,
}
