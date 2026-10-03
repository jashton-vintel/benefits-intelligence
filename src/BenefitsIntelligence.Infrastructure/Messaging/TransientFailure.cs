using BenefitsIntelligence.Application.Persistence;

using Microsoft.EntityFrameworkCore.Storage;

namespace BenefitsIntelligence.Infrastructure.Messaging;

/// <summary>
/// Decides whether a failed message is worth redelivering. Anything not recognised here is
/// treated as permanent, so an unexpected bug cannot put a message into a redelivery loop.
/// </summary>
internal static class TransientFailure
{
    public static bool IsTransient(Exception exception) => exception switch
    {
        // Another writer changed the job between our read and save; a retry re-reads it.
        ConcurrencyConflictException => true,

        // EF's retry policy has already given up on a transient database error. Database errors
        // that reach us unwrapped were classified as non-transient by that policy, so are permanent.
        RetryLimitExceededException => true,

        TimeoutException => true,
        _ => false,
    };
}
