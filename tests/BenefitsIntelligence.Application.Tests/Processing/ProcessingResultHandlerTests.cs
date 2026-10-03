using System.Text.Json;

using BenefitsIntelligence.Application.Messaging;
using BenefitsIntelligence.Application.Persistence;
using BenefitsIntelligence.Application.Processing;
using BenefitsIntelligence.Application.Tests.Fakes;
using BenefitsIntelligence.Domain.Processing;

namespace BenefitsIntelligence.Application.Tests.Processing;

public class ProcessingResultHandlerTests
{
    private readonly FakeProcessingJobRepository _jobs = new();
    private readonly ProcessingResultHandler _handler;

    public ProcessingResultHandlerTests()
    {
        _handler = new ProcessingResultHandler(_jobs, TimeProvider.System);
    }

    [Fact]
    public async Task CompletionMarksJobCompletedAndSaves()
    {
        ProcessingJob job = QueuedJob();

        ProcessingResultOutcome outcome = await _handler.HandleCompletedAsync(CompletionFor(job), CancellationToken.None);

        Assert.Equal(ProcessingResultOutcome.Recorded, outcome);
        Assert.Equal(ProcessingStatus.Completed, job.Status);
        Assert.Equal(1, _jobs.SaveCount);
    }

    [Fact]
    public async Task DuplicateCompletionIsAcknowledgedWithoutSaving()
    {
        ProcessingJob job = QueuedJob();
        ProcessPolicyCompleted completion = CompletionFor(job);
        await _handler.HandleCompletedAsync(completion, CancellationToken.None);

        ProcessingResultOutcome outcome = await _handler.HandleCompletedAsync(completion, CancellationToken.None);

        Assert.Equal(ProcessingResultOutcome.AlreadyRecorded, outcome);
        Assert.Equal(1, _jobs.SaveCount);
    }

    [Fact]
    public async Task UnknownCorrelationIdHasNoMatchingJob()
    {
        ProcessingJob unrelated = ProcessingJob.Create(Guid.NewGuid(), DateTimeOffset.UtcNow);

        ProcessingResultOutcome outcome = await _handler.HandleCompletedAsync(CompletionFor(unrelated), CancellationToken.None);

        Assert.Equal(ProcessingResultOutcome.NoMatchingJob, outcome);
        Assert.Equal(0, _jobs.SaveCount);
    }

    [Fact]
    public async Task CompletionForDifferentPolicyHasNoMatchingJob()
    {
        ProcessingJob job = QueuedJob();
        ProcessPolicyCompleted completion = CompletionFor(job) with { PolicyId = Guid.NewGuid() };

        ProcessingResultOutcome outcome = await _handler.HandleCompletedAsync(completion, CancellationToken.None);

        Assert.Equal(ProcessingResultOutcome.NoMatchingJob, outcome);
        Assert.Equal(ProcessingStatus.Queued, job.Status);
    }

    [Fact]
    public async Task CompletionForFailedJobIsRejectedByDomain()
    {
        ProcessingJob job = QueuedJob();
        job.MarkFailed("INVALID_DOCUMENT", "Not a PDF.", DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<InvalidStatusTransitionException>(
            () => _handler.HandleCompletedAsync(CompletionFor(job), CancellationToken.None));
    }

    [Fact]
    public async Task ConcurrencyConflictPropagatesForRedelivery()
    {
        ProcessingJob job = QueuedJob();
        _jobs.FailOnSave = new ConcurrencyConflictException();

        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => _handler.HandleCompletedAsync(CompletionFor(job), CancellationToken.None));
    }

    private ProcessingJob QueuedJob()
    {
        ProcessingJob job = ProcessingJob.Create(Guid.NewGuid(), DateTimeOffset.UtcNow);
        job.MarkQueued(DateTimeOffset.UtcNow);
        _jobs.Add(job);
        return job;
    }

    private static ProcessPolicyCompleted CompletionFor(ProcessingJob job) =>
        new(
            MessageId: Guid.NewGuid(),
            CorrelationId: job.CorrelationId,
            SchemaVersion: MessageSerialization.SchemaVersion,
            TenantId: Guid.NewGuid(),
            PolicyId: job.PolicyId,
            Status: "completed",
            Extraction: JsonDocument.Parse("{}").RootElement);
}
