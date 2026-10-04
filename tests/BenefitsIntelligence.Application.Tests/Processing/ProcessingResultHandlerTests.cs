using BenefitsIntelligence.Application.Messaging;
using BenefitsIntelligence.Application.Persistence;
using BenefitsIntelligence.Application.Processing;
using BenefitsIntelligence.Application.Tests.Fakes;
using BenefitsIntelligence.Domain.Policies;
using BenefitsIntelligence.Domain.Processing;

namespace BenefitsIntelligence.Application.Tests.Processing;

public class ProcessingResultHandlerTests
{
    private readonly FakeProcessingJobRepository _jobs = new();
    private readonly Dictionary<Guid, BenefitPolicy> _policies = [];
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
    public async Task CompletionRecordsExtractionOnThePolicy()
    {
        ProcessingJob job = QueuedJob();

        await _handler.HandleCompletedAsync(CompletionFor(job), CancellationToken.None);

        BenefitPolicy policy = _policies[job.PolicyId];
        Assert.Equal("Atlas Healthcare", policy.Provider);
        Assert.Equal("Corporate Plus", policy.SchemeName);
        Assert.Equal(100.00m, policy.AnnualExcess);
    }

    [Fact]
    public async Task RedeliveredCompletionDoesNotOverwriteRecordedExtraction()
    {
        ProcessingJob job = QueuedJob();
        await _handler.HandleCompletedAsync(CompletionFor(job), CancellationToken.None);

        ProcessPolicyCompleted redelivered = CompletionFor(job) with
        {
            Extraction = new PolicyExtraction("Someone Else", "Other Plan", 999m),
        };
        await _handler.HandleCompletedAsync(redelivered, CancellationToken.None);

        Assert.Equal("Atlas Healthcare", _policies[job.PolicyId].Provider);
        Assert.Equal(100.00m, _policies[job.PolicyId].AnnualExcess);
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

    [Fact]
    public async Task FailureMarksJobFailedWithReasonAndSaves()
    {
        ProcessingJob job = QueuedJob();

        ProcessingResultOutcome outcome = await _handler.HandleFailedAsync(FailureFor(job), CancellationToken.None);

        Assert.Equal(ProcessingResultOutcome.Recorded, outcome);
        Assert.Equal(ProcessingStatus.Failed, job.Status);
        Assert.Equal("INVALID_DOCUMENT", job.FailureCode);
        Assert.Equal("The PDF is password protected.", job.FailureMessage);
        Assert.Equal(1, _jobs.SaveCount);
    }

    [Fact]
    public async Task DuplicateFailureIsAcknowledgedWithoutSaving()
    {
        ProcessingJob job = QueuedJob();
        ProcessPolicyFailed failure = FailureFor(job);
        await _handler.HandleFailedAsync(failure, CancellationToken.None);

        ProcessingResultOutcome outcome = await _handler.HandleFailedAsync(failure, CancellationToken.None);

        Assert.Equal(ProcessingResultOutcome.AlreadyRecorded, outcome);
        Assert.Equal(1, _jobs.SaveCount);
    }

    [Fact]
    public async Task FailureForCompletedJobIsRejectedByDomain()
    {
        ProcessingJob job = QueuedJob();
        job.MarkCompleted(DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<InvalidStatusTransitionException>(
            () => _handler.HandleFailedAsync(FailureFor(job), CancellationToken.None));
    }

    [Fact]
    public async Task OverlongFailureReasonIsTruncatedToStoredLength()
    {
        ProcessingJob job = QueuedJob();
        ProcessPolicyFailed failure = FailureFor(job) with { ErrorMessage = new string('x', 5000) };

        await _handler.HandleFailedAsync(failure, CancellationToken.None);

        Assert.Equal(ProcessingResultHandler.MaxFailureMessageLength, job.FailureMessage!.Length);
    }

    private static ProcessPolicyFailed FailureFor(ProcessingJob job) =>
        new(
            MessageId: Guid.NewGuid(),
            CorrelationId: job.CorrelationId,
            SchemaVersion: MessageSerialization.SchemaVersion,
            TenantId: Guid.NewGuid(),
            PolicyId: job.PolicyId,
            Status: "failed",
            ErrorCode: "INVALID_DOCUMENT",
            ErrorMessage: "The PDF is password protected.");

    private ProcessingJob QueuedJob()
    {
        BenefitPolicy policy = new(Guid.NewGuid(), Guid.NewGuid(), "Current policy", BenefitType.PrivateMedicalInsurance, Guid.NewGuid(), DateTimeOffset.UtcNow);
        _policies[policy.Id] = policy;
        _jobs.Add(policy);

        ProcessingJob job = ProcessingJob.Create(policy.Id, DateTimeOffset.UtcNow);
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
            Document: new DocumentSummary(PageCount: 7, ChunkCount: 8),
            Extraction: new PolicyExtraction("Atlas Healthcare", "Corporate Plus", 100.00m));
}
