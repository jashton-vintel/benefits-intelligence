using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

using BenefitsIntelligence.Application.Messaging;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BenefitsIntelligence.Infrastructure.PythonApi;

/// <summary>
/// Calls the internal Python API. Its features are optional or have a fallback, so a failed call
/// is logged and reported as no result rather than thrown.
/// </summary>
internal sealed partial class PythonApiClient(
    HttpClient http,
    IOptions<PythonApiOptions> options,
    ILogger<PythonApiClient> logger)
{
    public const string InternalKeyHeader = "X-Internal-Key";
    public const string CorrelationHeader = "X-Correlation-ID";

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken)
        where TResponse : class
    {
        PythonApiOptions settings = options.Value;
        if (!settings.IsConfigured)
        {
            LogNotConfigured(path);
            return null;
        }

        // Same snake_case contract as the messages exchanged with the worker.
        using HttpRequestMessage message = new(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, options: MessageSerialization.Options),
        };
        message.Headers.Add(InternalKeyHeader, settings.ApiKey);

        if (Activity.Current is { } activity)
        {
            message.Headers.Add(CorrelationHeader, activity.TraceId.ToString());
        }

        try
        {
            using HttpResponseMessage response = await http.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogUnsuccessful(path, (int)response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TResponse>(MessageSerialization.Options, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient reports its own timeout as a cancellation; only the caller's is passed on.
            LogTimedOut(path, http.Timeout.TotalSeconds);
            return null;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            LogFailed(exception, path);
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "No Python API is configured; {Path} was not called")]
    private partial void LogNotConfigured(string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Python API {Path} returned status {StatusCode}")]
    private partial void LogUnsuccessful(string path, int statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Python API {Path} timed out after {Seconds} seconds")]
    private partial void LogTimedOut(string path, double seconds);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Python API {Path} request failed")]
    private partial void LogFailed(Exception exception, string path);
}
