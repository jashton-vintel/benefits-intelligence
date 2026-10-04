using System.Net;
using System.Text.Json;

using BenefitsIntelligence.Application.Comparison;
using BenefitsIntelligence.Application.Messaging;
using BenefitsIntelligence.Application.Questions;
using BenefitsIntelligence.Domain.Comparison;
using BenefitsIntelligence.Infrastructure.PythonApi;

namespace BenefitsIntelligence.Infrastructure.Tests.PythonApi;

public class PythonFeatureClientTests
{
    [Fact]
    public async Task SummariserPostsTheComparisonAndReturnsTheSummary()
    {
        StubHandler handler = StubHandler.Responding(HttpStatusCode.OK, """{"summary":"The excess increases from £100 to £150."}""");
        ComparisonSummaryRequest request = new(
            new PolicyReference(Guid.NewGuid(), "Current", "Atlas Healthcare", "Corporate Plus"),
            new PolicyReference(Guid.NewGuid(), "Proposed", "NorthStar Health", "Essentials Select"),
            [new FieldDifference("annual_excess", ValueKind.Money, "100.00", "150.00", 50m, Change.Increased, false)]);

        string? summary = await new PythonComparisonSummariser(handler.Client()).SummariseAsync(request, CancellationToken.None);

        Assert.Equal("The excess increases from £100 to £150.", summary);
        Assert.Equal("/summaries/comparison", handler.Requests.Single().RequestUri!.AbsolutePath);
        using JsonDocument body = JsonDocument.Parse(handler.Bodies.Single());
        Assert.Equal("increased", body.RootElement.GetProperty("differences")[0].GetProperty("change").GetString());
    }

    [Fact]
    public async Task BlankSummaryIsTreatedAsNoSummary()
    {
        StubHandler handler = StubHandler.Responding(HttpStatusCode.OK, """{"summary":"  "}""");
        ComparisonSummaryRequest request = new(
            new PolicyReference(Guid.NewGuid(), "Current", null, null),
            new PolicyReference(Guid.NewGuid(), "Proposed", null, null),
            []);

        Assert.Null(await new PythonComparisonSummariser(handler.Client()).SummariseAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task AnswererSendsThePagesAndReadsTheCitedAnswer()
    {
        StubHandler handler = StubHandler.Responding(
            HttpStatusCode.OK,
            """{"answer":"Yes, up to ten sessions.","supported":true,"citations":[{"page_start":4,"page_end":4,"quote":"up to ten sessions"}]}""");
        PolicyQuestionRequest request = new("Is physiotherapy covered?", "Proposed policy", [new PageContent(1, "Page one text")]);

        PolicyAnswer? answer = await new PythonQuestionAnswerer(handler.Client()).AnswerAsync(request, CancellationToken.None);

        Assert.NotNull(answer);
        Assert.Equal(("Yes, up to ten sessions.", true), (answer.Answer, answer.Supported));
        Assert.Equal([new AnswerCitation(4, 4, "up to ten sessions")], answer.Citations);

        Assert.Equal("/answers", handler.Requests.Single().RequestUri!.AbsolutePath);
        using JsonDocument body = JsonDocument.Parse(handler.Bodies.Single());
        Assert.Equal("Proposed policy", body.RootElement.GetProperty("policy_name").GetString());
        Assert.Equal(1, body.RootElement.GetProperty("pages")[0].GetProperty("page_number").GetInt32());
    }
}
