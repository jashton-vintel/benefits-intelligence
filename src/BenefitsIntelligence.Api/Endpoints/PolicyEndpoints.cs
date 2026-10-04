using BenefitsIntelligence.Application.Documents;
using BenefitsIntelligence.Application.Policies;
using BenefitsIntelligence.Application.Questions;
using BenefitsIntelligence.Domain.Policies;
using BenefitsIntelligence.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc;

namespace BenefitsIntelligence.Api.Endpoints;

internal static class PolicyEndpoints
{
    private const string GetPolicyRouteName = "GetPolicy";
    private const int MaxNameLength = 200;
    private const int MaxFileNameLength = 255;
    private const long MaxDocumentBytes = 20 * 1024 * 1024;

    // TODO: take the organisation from the authenticated caller's claims once authentication is in place.
    private static readonly Guid OrganisationId = SeededOrganisation.Id;

    public static IEndpointRouteBuilder MapPolicyEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder policies = app.MapGroup("/api/policies");

        // Callers authenticate with bearer tokens rather than cookies, so form posts are not
        // exposed to cross-site request forgery.
        policies.MapPost("/", UploadAsync).DisableAntiforgery();
        policies.MapGet("/", ListAsync);
        policies.MapGet("/{id:guid}", GetAsync).WithName(GetPolicyRouteName);
        policies.MapPost("/{id:guid}/questions", AskAsync);

        return app;
    }

    private static async Task<IResult> UploadAsync(
        [FromForm] string? name,
        IFormFile? file,
        PolicyUploadService uploads,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaxNameLength)
        {
            return Invalid("name", $"A policy name of up to {MaxNameLength} characters is required.");
        }

        if (file is null || file.Length == 0)
        {
            return Invalid("file", "A PDF document is required.");
        }

        if (file.Length > MaxDocumentBytes)
        {
            return Invalid("file", $"The document must be no larger than {MaxDocumentBytes / (1024 * 1024)} MB.");
        }

        await using Stream content = file.OpenReadStream();

        if (!await PdfSignature.MatchesAsync(content, cancellationToken))
        {
            return Invalid("file", "The file is not a PDF document.");
        }

        UploadPolicyCommand command = new(
            OrganisationId,
            name.Trim(),
            BenefitType.PrivateMedicalInsurance,
            SafeFileName(file.FileName),
            content,
            file.Length);

        UploadPolicyResult result = await uploads.UploadAsync(command, cancellationToken);

        return Results.AcceptedAtRoute(GetPolicyRouteName, new { id = result.PolicyId }, result);
    }

    private static async Task<IResult> ListAsync(IPolicyQueries queries, CancellationToken cancellationToken) =>
        Results.Ok(await queries.ListAsync(OrganisationId, cancellationToken));

    private static async Task<IResult> GetAsync(Guid id, IPolicyQueries queries, CancellationToken cancellationToken) =>
        await queries.FindAsync(OrganisationId, id, cancellationToken) is { } policy
            ? Results.Ok(policy)
            : Results.NotFound();

    private static async Task<IResult> AskAsync(
        Guid id,
        AskQuestionRequest request,
        PolicyQuestionService questions,
        CancellationToken cancellationToken)
    {
        string? question = request.Question?.Trim();
        if (string.IsNullOrEmpty(question) || question.Length > PolicyQuestionService.MaxQuestionLength)
        {
            return Invalid("question", $"A question of up to {PolicyQuestionService.MaxQuestionLength} characters is required.");
        }

        QuestionResult result = await questions.AskAsync(OrganisationId, id, question, cancellationToken);

        return result.Status switch
        {
            QuestionStatus.Answered => Results.Ok(result.Answer),
            QuestionStatus.PolicyNotFound => Results.NotFound(),
            QuestionStatus.PolicyNotReady => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Policy not ready",
                detail: "The policy has not finished processing, so questions cannot be answered yet."),
            QuestionStatus.TextUnavailable => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Policy text unavailable",
                detail: "This policy was processed before its text was kept for questions. Upload it again to ask about it."),
            _ => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Answers unavailable",
                detail: "Questions cannot be answered right now. Please try again shortly."),
        };
    }

    private static IResult Invalid(string field, string message) => Results.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });

    private static string SafeFileName(string fileName)
    {
        string name = Path.GetFileName(fileName);

        if (string.IsNullOrWhiteSpace(name))
        {
            return "document.pdf";
        }

        return name.Length <= MaxFileNameLength ? name : name[..MaxFileNameLength];
    }
}
