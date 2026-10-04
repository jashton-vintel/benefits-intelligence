using Microsoft.Extensions.Logging;

namespace BenefitsIntelligence.Infrastructure.Messaging;

internal static class MessageLogScope
{
    // Snake-case keys match the Python worker's log fields so one query spans both services.
    public static IDisposable? Begin(ILogger logger, string? correlationId, string? messageId) =>
        logger.BeginScope(new LogFields(
            new("correlation_id", correlationId),
            new("message_id", messageId)));
}