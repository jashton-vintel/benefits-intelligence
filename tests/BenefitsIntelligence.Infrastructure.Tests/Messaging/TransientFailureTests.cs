using System.Text.Json;

using BenefitsIntelligence.Application.Persistence;
using BenefitsIntelligence.Domain.Processing;
using BenefitsIntelligence.Infrastructure.Messaging;

using Microsoft.EntityFrameworkCore.Storage;

namespace BenefitsIntelligence.Infrastructure.Tests.Messaging;

public class TransientFailureTests
{
    public static TheoryData<Exception> Transient =>
    [
        new ConcurrencyConflictException(),
        new RetryLimitExceededException("Retries exhausted.", new InvalidOperationException()),
        new TimeoutException(),
    ];

    public static TheoryData<Exception> Permanent =>
    [
        new JsonException(),
        new InvalidStatusTransitionException(ProcessingStatus.Failed, ProcessingStatus.Completed),
        new InvalidOperationException(),
        new ArgumentException(),
    ];

    [Theory]
    [MemberData(nameof(Transient))]
    public void RecognisesTransientFailures(Exception exception) =>
        Assert.True(TransientFailure.IsTransient(exception));

    [Theory]
    [MemberData(nameof(Permanent))]
    public void TreatsEverythingElseAsPermanent(Exception exception) =>
        Assert.False(TransientFailure.IsTransient(exception));
}
