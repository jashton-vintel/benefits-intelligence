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
        _handler = new ProcessingResultHandler(_jobs, new ReviewPolicy(confidenceThreshold: 0.8), TimeProvider.System);
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
        Assert.Equal("NorthStar Health", policy.Provider);
        Assert.Equal("Essentials Select", policy.SchemeName);
        Assert.Equal(108000.00m, policy.AnnualPremium);
        Assert.Equal(150.00m, policy.AnnualExcess);
        Assert.Equal(new DateOnly(2027, 4, 1), policy.EffectiveDate);
        Assert.Null(policy.DependantsIncluded);
        Assert.Equal(10, policy.CoverageItems.Single(c => c.Type == CoverageType.Physiotherapy).SessionLimit);
        Assert.Equal("0", policy.EligibilityRules.Single(r => r.Type == EligibilityRuleType.MinimumServiceMonths).Value);
    }

    [Fact]
    public async Task CompletionRecordsWhereEachFactWasFound()
    {
        ProcessingJob job = QueuedJob();

        await _handler.HandleCompletedAsync(CompletionFor(job), CancellationToken.None);

        FactAssessment excess = FieldAssessment(_policies[job.PolicyId], PolicyField.AnnualExcess);
        Assert.Equal(new Evidence(3, 3, "An excess of £150 applies to each covered person once in each scheme year"), excess.Evidence);
        Assert.Equal(0.95, excess.Confidence);
        Assert.False(excess.NeedsReview);
    }

    [Fact]
    public async Task AmbiguousFactPutsThePolicyUpForReview()
    {
        ProcessingJob job = QueuedJob();

        await _handler.HandleCompletedAsync(CompletionFor(job), CancellationToken.None);

        BenefitPolicy policy = _policies[job.PolicyId];
        Assert.True(policy.NeedsReview);
        Assert.Equal(
            ReviewReasons.Ambiguous | ReviewReasons.LowConfidence,
            FieldAssessment(policy, PolicyField.DependantsIncluded).ReviewReasons);
    }

    [Fact]
    public async Task PolicyWithoutProblemsDoesNotNeedReview()
    {
        ProcessingJob job = QueuedJob();
        ProcessPolicyCompleted completion = CompletionFor(job);
        PolicyExtraction extraction = completion.Extraction;
        completion = completion with
        {
            Extraction = extraction with
            {
                DependantsIncluded = extraction.DependantsIncluded with { Confidence = 0.95, Issues = [] },
            },
        };

        await _handler.HandleCompletedAsync(completion, CancellationToken.None);

        Assert.False(_policies[job.PolicyId].NeedsReview);
    }

    [Fact]
    public async Task ConfidenceThresholdComesFromTheReviewPolicy()
    {
        ProcessingJob job = QueuedJob();
        ProcessingResultHandler strictHandler = new(_jobs, new ReviewPolicy(confidenceThreshold: 0.9), TimeProvider.System);

        await strictHandler.HandleCompletedAsync(CompletionFor(job), CancellationToken.None);

        EligibilityRule employmentType = _policies[job.PolicyId].EligibilityRules.Single(r => r.Type == EligibilityRuleType.EmploymentType);
        Assert.Equal(ReviewReasons.LowConfidence, employmentType.Assessment.ReviewReasons);
    }

    [Fact]
    public async Task UnsupportedExtractionSchemaIsRejected()
    {
        ProcessingJob job = QueuedJob();
        ProcessPolicyCompleted completion = CompletionFor(job);
        completion = completion with { Extraction = completion.Extraction with { SchemaVersion = "2.0" } };

        await Assert.ThrowsAsync<NotSupportedException>(() => _handler.HandleCompletedAsync(completion, CancellationToken.None));
        Assert.Equal(0, _jobs.SaveCount);
    }

    [Fact]
    public async Task RedeliveredCompletionDoesNotOverwriteRecordedExtraction()
    {
        ProcessingJob job = QueuedJob();
        await _handler.HandleCompletedAsync(CompletionFor(job), CancellationToken.None);

        ProcessPolicyCompleted redelivered = CompletionFor(job);
        redelivered = redelivered with
        {
            Extraction = redelivered.Extraction with
            {
                Provider = redelivered.Extraction.Provider with { Value = "Someone Else" },
            },
        };
        await _handler.HandleCompletedAsync(redelivered, CancellationToken.None);

        Assert.Equal("NorthStar Health", _policies[job.PolicyId].Provider);
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
        ContractFixtures.Deserialize<ProcessPolicyCompleted>("process_completed.json") with
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = job.CorrelationId,
            PolicyId = job.PolicyId,
        };

    private static FactAssessment FieldAssessment(BenefitPolicy policy, PolicyField field) =>
        policy.FieldAssessments.Single(a => a.Field == field).Assessment;
}
