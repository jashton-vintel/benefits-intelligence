using BenefitsIntelligence.Application.Comparison;
using BenefitsIntelligence.Infrastructure.Persistence;

namespace BenefitsIntelligence.Api.Endpoints;

internal static class ComparisonEndpoints
{
    // TODO: take the organisation from the authenticated caller's claims once authentication is in place.
    private static readonly Guid OrganisationId = SeededOrganisation.Id;

    public static IEndpointRouteBuilder MapComparisonEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/comparisons", CompareAsync);
        return app;
    }

    private static async Task<IResult> CompareAsync(
        Guid current,
        Guid proposed,
        bool? includeSummary,
        PolicyComparisonService comparisons,
        CancellationToken cancellationToken)
    {
        if (current == proposed)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["proposed"] = ["Choose two different policies to compare."],
            });
        }

        // The summary takes seconds to write, so it is only produced when asked for; the
        // calculated comparison is returned either way.
        ComparisonResult result = await comparisons.CompareAsync(
            OrganisationId,
            current,
            proposed,
            includeSummary ?? false,
            cancellationToken);

        return result.Status switch
        {
            ComparisonStatus.Compared => Results.Ok(result.Report),
            ComparisonStatus.PolicyNotFound => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Policy not found",
                detail: $"Policy {result.PolicyId} does not exist."),
            _ => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Policy not ready",
                detail: $"Policy {result.PolicyId} has not finished processing, so it cannot be compared yet."),
        };
    }
}
