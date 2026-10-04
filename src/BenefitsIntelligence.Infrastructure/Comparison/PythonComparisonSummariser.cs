using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

using BenefitsIntelligence.Application.Comparison;
using BenefitsIntelligence.Application.Messaging;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BenefitsIntelligence.Infrastructure.Comparison;

internal sealed partial class PythonComparisonSummariser(
    HttpClient http,
    IOptions<PythonApiOptions> options,
    ILogger<PythonComparisonSummariser> logger) : IComparisonSummariser
{
    public const string InternalKeyHeader = "X-Internal-Key";
    public const string CorrelationHeader = "X-Correlation-ID";

    public async Task<string?> SummariseAsync(ComparisonSummaryRequest request, CancellationToken cancellationToken)
    {
        PythonApiOptions settings = options.Value;
        if (!settings.IsConfigured)
        {
            LogNotConfigured();
            return null;
        }

        // Same snake_case contract as the messages exchanged with the worker.
        using HttpRequestMessage message = new(HttpMethod.Post, "summaries/comparison")
        {
            Content = JsonContent.Create(request, options: MessageSerialization.Options),
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
                LogUnavailable((int)response.StatusCode);
                return null;
            }

            ComparisonSummaryResponse? body = await response.Content.ReadFromJsonAsync<ComparisonSummaryResponse>(
                MessageSerialization.Options,
                cancellationToken);

            return string.IsNullOrWhiteSpace(body?.Summary) ? null : body.Summary;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient reports its own timeout as a cancellation; only the caller's is passed on.
            LogTimedOut(http.Timeout.TotalSeconds);
            return null;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            LogFailed(exception);
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "No Python API is configured; comparisons are returned without a summary")]
    private partial void LogNotConfigured();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Comparison summary unavailable (status {StatusCode})")]
    private partial void LogUnavailable(int statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Comparison summary timed out after {Seconds} seconds")]
    private partial void LogTimedOut(double seconds);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Comparison summary request failed")]
    private partial void LogFailed(Exception exception);
}
