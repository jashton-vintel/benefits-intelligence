using BenefitsIntelligence.Domain.Processing;

namespace BenefitsIntelligence.Domain.Tests.Processing;

public class ProcessingJobTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = CreatedAt.AddMinutes(5);
    private static readonly DateTimeOffset MuchLater = CreatedAt.AddMinutes(10);

    [Fact]
    public void NewJobStartsUploadedWithItsOwnCorrelationId()
    {
        Guid policyId = Guid.NewGuid();

        ProcessingJob job = ProcessingJob.Create(policyId, CreatedAt);

        Assert.Equal(ProcessingStatus.Uploaded, job.Status);
        Assert.Equal(policyId, job.PolicyId);
        Assert.NotEqual(Guid.Empty, job.CorrelationId);
        Assert.Equal(CreatedAt, job.UpdatedAt);
        Assert.False(job.IsFinished);
    }

    [Fact]
    public void MarkQueuedMovesUploadedJobToQueued()
    {
        ProcessingJob job = ProcessingJob.Create(Guid.NewGuid(), CreatedAt);

        bool changed = job.MarkQueued(Later);

        Assert.True(changed);
        Assert.Equal(ProcessingStatus.Queued, job.Status);
        Assert.Equal(Later, job.UpdatedAt);
    }

    [Theory]
    [InlineData(ProcessingStatus.Queued)]
    [InlineData(ProcessingStatus.Completed)]
    [InlineData(ProcessingStatus.Failed)]
    public void MarkQueuedIsIgnoredOnceJobHasMovedOn(ProcessingStatus status)
    {
        ProcessingJob job = JobIn(status);

        bool changed = job.MarkQueued(MuchLater);

        Assert.False(changed);
        Assert.Equal(status, job.Status);
        Assert.Equal(Later, job.UpdatedAt);
    }

    [Theory]
    [InlineData(ProcessingStatus.Uploaded)]
    [InlineData(ProcessingStatus.Queued)]
    public void MarkCompletedCompletesUnfinishedJob(ProcessingStatus status)
    {
        ProcessingJob job = JobIn(status);

        bool changed = job.MarkCompleted(MuchLater);

        Assert.True(changed);
        Assert.Equal(ProcessingStatus.Completed, job.Status);
        Assert.Equal(MuchLater, job.UpdatedAt);
        Assert.True(job.IsFinished);
    }

    [Fact]
    public void DuplicateCompletionIsIgnored()
    {
        ProcessingJob job = JobIn(ProcessingStatus.Completed);

        bool changed = job.MarkCompleted(MuchLater);

        Assert.False(changed);
        Assert.Equal(ProcessingStatus.Completed, job.Status);
        Assert.Equal(Later, job.UpdatedAt);
    }

    [Fact]
    public void CompletingFailedJobIsRejected()
    {
        ProcessingJob job = JobIn(ProcessingStatus.Failed);

        InvalidStatusTransitionException exception =
            Assert.Throws<InvalidStatusTransitionException>(() => job.MarkCompleted(MuchLater));

        Assert.Equal(ProcessingStatus.Failed, exception.From);
        Assert.Equal(ProcessingStatus.Completed, exception.To);
        Assert.Equal(ProcessingStatus.Failed, job.Status);
    }

    [Theory]
    [InlineData(ProcessingStatus.Uploaded)]
    [InlineData(ProcessingStatus.Queued)]
    public void MarkFailedRecordsFailureOnUnfinishedJob(ProcessingStatus status)
    {
        ProcessingJob job = JobIn(status);

        bool changed = job.MarkFailed("INVALID_DOCUMENT", "Not a PDF.", MuchLater);

        Assert.True(changed);
        Assert.Equal(ProcessingStatus.Failed, job.Status);
        Assert.Equal("INVALID_DOCUMENT", job.FailureCode);
        Assert.Equal("Not a PDF.", job.FailureMessage);
        Assert.Equal(MuchLater, job.UpdatedAt);
        Assert.True(job.IsFinished);
    }

    [Fact]
    public void DuplicateFailureIsIgnoredAndKeepsOriginalReason()
    {
        ProcessingJob job = JobIn(ProcessingStatus.Failed);

        bool changed = job.MarkFailed("EXTRACTION_FAILED", "Different reason.", MuchLater);

        Assert.False(changed);
        Assert.Equal("INVALID_DOCUMENT", job.FailureCode);
        Assert.Equal(Later, job.UpdatedAt);
    }

    [Fact]
    public void FailingCompletedJobIsRejected()
    {
        ProcessingJob job = JobIn(ProcessingStatus.Completed);

        Assert.Throws<InvalidStatusTransitionException>(() => job.MarkFailed("EXTRACTION_FAILED", "Too late.", MuchLater));
        Assert.Equal(ProcessingStatus.Completed, job.Status);
        Assert.Null(job.FailureCode);
    }

    [Theory]
    [InlineData("", "message")]
    [InlineData("CODE", " ")]
    public void MarkFailedRequiresCodeAndMessage(string code, string message)
    {
        ProcessingJob job = JobIn(ProcessingStatus.Queued);

        Assert.ThrowsAny<ArgumentException>(() => job.MarkFailed(code, message, MuchLater));
        Assert.Equal(ProcessingStatus.Queued, job.Status);
    }

    private static ProcessingJob JobIn(ProcessingStatus status)
    {
        ProcessingJob job = ProcessingJob.Create(Guid.NewGuid(), CreatedAt);

        switch (status)
        {
            case ProcessingStatus.Uploaded:
                break;
            case ProcessingStatus.Queued:
                job.MarkQueued(Later);
                break;
            case ProcessingStatus.Completed:
                job.MarkCompleted(Later);
                break;
            case ProcessingStatus.Failed:
                job.MarkFailed("INVALID_DOCUMENT", "Not a PDF.", Later);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        return job;
    }
}
