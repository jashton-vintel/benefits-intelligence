using System.Net;
using System.Text;

using BenefitsIntelligence.Infrastructure.PythonApi;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BenefitsIntelligence.Infrastructure.Tests.PythonApi;

internal sealed class StubHandler(Func<CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    public const string Key = "kkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkk";

    public List<HttpRequestMessage> Requests { get; } = [];

    public List<string> Bodies { get; } = [];

    public static StubHandler Responding(HttpStatusCode status, string json) =>
        new(_ => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        }));

    public PythonApiClient Client(TimeSpan? timeout = null, bool configured = true)
    {
        HttpClient http = new(this)
        {
            BaseAddress = new Uri("http://ai-api:8000/"),
            Timeout = timeout ?? TimeSpan.FromSeconds(5),
        };
        PythonApiOptions options = configured
            ? new PythonApiOptions { BaseUrl = "http://ai-api:8000", ApiKey = Key }
            : new PythonApiOptions();

        return new PythonApiClient(http, Options.Create(options), NullLogger<PythonApiClient>.Instance);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        Bodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
        return await respond(cancellationToken);
    }
}
