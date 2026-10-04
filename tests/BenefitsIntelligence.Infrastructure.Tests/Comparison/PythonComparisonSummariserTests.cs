using System.Net;
using System.Text;
using System.Text.Json;

using BenefitsIntelligence.Application.Comparison;
using BenefitsIntelligence.Domain.Comparison;
using BenefitsIntelligence.Infrastructure.Comparison;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BenefitsIntelligence.Infrastructure.Tests.Comparison;

public class PythonComparisonSummariserTests
{
    private const string Key = "kkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkk";

    private static readonly ComparisonSummaryRequest Request = new(
        new PolicyReference(Guid.NewGuid(), "Current", "Atlas Healthcare", "Corporate Plus"),
        new PolicyReference(Guid.NewGuid(), "Proposed", "NorthStar Health", "Essentials Select"),
        [new FieldDifference("annual_excess", ValueKind.Money, "100.00", "150.00", 50m, Change.Increased, false)]);

    [Fact]
    public async Task SendsTheComparisonWithTheInternalKeyAndReturnsTheSummary()
    {
        StubHandler handler = Responding(HttpStatusCode.OK, """{"summary":"The excess increases from £100 to £150."}""");

        string? summary = await Summariser(handler).SummariseAsync(Request, CancellationToken.None);

        Assert.Equal("The excess increases from £100 to £150.", summary);
        HttpRequestMessage sent = Assert.Single(handler.Requests);
        Assert.Equal(new Uri("http://ai-api:8000/summaries/comparison"), sent.RequestUri);
        Assert.Equal([Key], sent.Headers.GetValues(PythonComparisonSummariser.InternalKeyHeader));

        using JsonDocument body = JsonDocument.Parse(handler.Bodies.Single());
        JsonElement difference = body.RootElement.GetProperty("differences")[0];
        Assert.Equal("increased", difference.GetProperty("change").GetString());
        Assert.Equal("money", difference.GetProperty("kind").GetString());
        Assert.False(difference.GetProperty("needs_review").GetBoolean());
        Assert.Equal("Corporate Plus", body.RootElement.GetProperty("current").GetProperty("scheme_name").GetString());
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task UnsuccessfulResponseMeansNoSummary(HttpStatusCode status)
    {
        StubHandler handler = Responding(status, """{"detail":"No summary."}""");

        Assert.Null(await Summariser(handler).SummariseAsync(Request, CancellationToken.None));
    }

    [Fact]
    public async Task UnreachableServiceMeansNoSummary()
    {
        StubHandler handler = new(_ => throw new HttpRequestException("Connection refused."));

        Assert.Null(await Summariser(handler).SummariseAsync(Request, CancellationToken.None));
    }

    [Fact]
    public async Task SlowServiceTimesOutWithoutFailingTheComparison()
    {
        StubHandler handler = new(async cancellationToken =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            return Json(HttpStatusCode.OK, """{"summary":"Too late."}""");
        });

        string? summary = await Summariser(handler, TimeSpan.FromMilliseconds(50)).SummariseAsync(Request, CancellationToken.None);

        Assert.Null(summary);
    }

    [Fact]
    public async Task CallersOwnCancellationIsPassedOn()
    {
        StubHandler handler = new(async cancellationToken =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            return Json(HttpStatusCode.OK, "{}");
        });
        using CancellationTokenSource cancellation = new(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Summariser(handler).SummariseAsync(Request, cancellation.Token));
    }

    [Fact]
    public async Task WithoutConfigurationNoRequestIsMade()
    {
        StubHandler handler = Responding(HttpStatusCode.OK, """{"summary":"Unexpected."}""");
        PythonComparisonSummariser summariser = new(
            new HttpClient(handler),
            Options.Create(new PythonApiOptions()),
            NullLogger<PythonComparisonSummariser>.Instance);

        Assert.Null(await summariser.SummariseAsync(Request, CancellationToken.None));
        Assert.Empty(handler.Requests);
    }

    private static PythonComparisonSummariser Summariser(StubHandler handler, TimeSpan? timeout = null)
    {
        HttpClient client = new(handler)
        {
            BaseAddress = new Uri("http://ai-api:8000/"),
            Timeout = timeout ?? TimeSpan.FromSeconds(5),
        };

        return new PythonComparisonSummariser(
            client,
            Options.Create(new PythonApiOptions { BaseUrl = "http://ai-api:8000", ApiKey = Key }),
            NullLogger<PythonComparisonSummariser>.Instance);
    }

    private static StubHandler Responding(HttpStatusCode status, string json) =>
        new(_ => Task.FromResult(Json(status, json)));

    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Bodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
            return await respond(cancellationToken);
        }
    }
}
