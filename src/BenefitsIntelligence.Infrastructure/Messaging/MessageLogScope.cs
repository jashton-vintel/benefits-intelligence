using Microsoft.Extensions.Logging;

namespace BenefitsIntelligence.Infrastructure.Messaging;

internal static class MessageLogScope
{
    // Snake-case keys match the Python worker's log fields so one query spans both services.
    public static IDisposable? Begin(ILogger logger, string? correlationId, string? messageId) =>
        logger.BeginScope(new Dictionary<string, object?>
        {
            ["correlation_id"] = correlationId,
            ["message_id"] = messageId,
        });
}