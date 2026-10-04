using System.Net;
using System.Text.Json;

using BenefitsIntelligence.Infrastructure.PythonApi;

namespace BenefitsIntelligence.Infrastructure.Tests.PythonApi;

public class PythonApiClientTests
{
    private static readonly SampleRequest Request = new("Is physiotherapy covered?", NeedsReview: true);

    [Fact]
    public async Task PostsSnakeCaseJsonWithTheInternalKey()
    {
        StubHandler handler = StubHandler.Responding(HttpStatusCode.OK, """{"reply_text":"Yes."}""");

        SampleResponse? response = await handler.Client().PostAsync<SampleRequest, SampleResponse>("answers", Request, CancellationToken.None);

        Assert.Equal(new SampleResponse("Yes."), response);
        HttpRequestMessage sent = Assert.Single(handler.Requests);
        Assert.Equal(new Uri("http://ai-api:8000/answers"), sent.RequestUri);
        Assert.Equal([StubHandler.Key], sent.Headers.GetValues(PythonApiClient.InternalKeyHeader));

        using JsonDocument body = JsonDocument.Parse(handler.Bodies.Single());
        Assert.True(body.RootElement.GetProperty("needs_review").GetBoolean());
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public async Task UnsuccessfulResponseMeansNoResult(HttpStatusCode status)
    {
        StubHandler handler = StubHandler.Responding(status, """{"detail":"No."}""");

        Assert.Null(await handler.Client().PostAsync<SampleRequest, SampleResponse>("answers", Request, CancellationToken.None));
    }

    [Fact]
    public async Task UnreachableServiceMeansNoResult()
    {
        StubHandler handler = new(_ => throw new HttpRequestException("Connection refused."));

        Assert.Null(await handler.Client().PostAsync<SampleRequest, SampleResponse>("answers", Request, CancellationToken.None));
    }

    [Fact]
    public async Task MalformedResponseMeansNoResult()
    {
        StubHandler handler = StubHandler.Responding(HttpStatusCode.OK, "not json");

        Assert.Null(await handler.Client().PostAsync<SampleRequest, SampleResponse>("answers", Request, CancellationToken.None));
    }

    [Fact]
    public async Task SlowServiceTimesOutWithoutThrowing()
    {
        StubHandler handler = new(async cancellationToken =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        SampleResponse? response = await handler.Client(TimeSpan.FromMilliseconds(50))
            .PostAsync<SampleRequest, SampleResponse>("answers", Request, CancellationToken.None);

        Assert.Null(response);
    }

    [Fact]
    public async Task CallersOwnCancellationIsPassedOn()
    {
        StubHandler handler = new(async cancellationToken =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using CancellationTokenSource cancellation = new(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.Client().PostAsync<SampleRequest, SampleResponse>("answers", Request, cancellation.Token));
    }

    [Fact]
    public async Task WithoutConfigurationNoRequestIsMade()
    {
        StubHandler handler = StubHandler.Responding(HttpStatusCode.OK, """{"reply_text":"Unexpected."}""");

        Assert.Null(await handler.Client(configured: false).PostAsync<SampleRequest, SampleResponse>("answers", Request, CancellationToken.None));
        Assert.Empty(handler.Requests);
    }

    private sealed record SampleRequest(string Question, bool NeedsReview);

    private sealed record SampleResponse(string ReplyText);
}
