using BenefitsIntelligence.Application.Messaging;
using BenefitsIntelligence.Application.Persistence;
using BenefitsIntelligence.Application.Policies;
using BenefitsIntelligence.Application.Tests.Fakes;
using BenefitsIntelligence.Domain.Policies;
using BenefitsIntelligence.Domain.Processing;

namespace BenefitsIntelligence.Application.Tests.Policies;

public class PolicyUploadServiceTests
{
    private static readonly Guid OrganisationId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly CallLog _log = new();
    private readonly FakeDocumentStore _store;
    private readonly FakePolicyRepository _repository;
    private readonly FakeProcessingRequestPublisher _publisher;
    private readonly PolicyUploadService _service;

    public PolicyUploadServiceTests()
    {
        _store = new FakeDocumentStore(_log);
        _repository = new FakePolicyRepository(_log);
        _publisher = new FakeProcessingRequestPublisher(_log);
        _service = new PolicyUploadService(_store, _repository, _publisher, TimeProvider.System);
    }

    [Fact]
    public async Task StoresFileThenRecordsThenPublishesThenMarksQueued()
    {
        await _service.UploadAsync(Command(), CancellationToken.None);

        Assert.Equal(
            ["store.save", "repository.save(Uploaded)", "publish", "repository.save(Queued)"],
            _log.Calls);
        Assert.Equal(ProcessingStatus.Queued, _repository.Job!.Status);
    }

    [Fact]
    public async Task PublishedRequestDescribesTheStoredDocumentAndJob()
    {
        UploadPolicyResult result = await _service.UploadAsync(Command(), CancellationToken.None);

        ProcessPolicyRequested message = Assert.Single(_publisher.Published);
        Assert.Equal(result.PolicyId, message.PolicyId);
        Assert.Equal(result.CorrelationId, message.CorrelationId);
        Assert.Equal(_repository.Job!.CorrelationId, message.CorrelationId);
        Assert.Equal(OrganisationId, message.TenantId);
        Assert.Equal(_repository.Document!.Id, message.DocumentId);
        Assert.Equal(_repository.Document.StoragePath, message.DocumentLocation);
        Assert.Equal(MessageSerialization.SchemaVersion, message.SchemaVersion);
    }

    [Fact]
    public async Task RecordsUploadDetailsOnDocumentAndPolicy()
    {
        await _service.UploadAsync(Command(), CancellationToken.None);

        Assert.Equal("CurrentHealthPolicy.pdf", _repository.Document!.FileName);
        Assert.Equal("application/pdf", _repository.Document.ContentType);
        Assert.Equal(1234, _repository.Document.SizeBytes);
        Assert.Equal("Current policy", _repository.Policy!.Name);
        Assert.Equal(_repository.Document.Id, _repository.Policy.DocumentId);
    }

    [Fact]
    public async Task FailedRecordSaveRemovesStoredFileAndPublishesNothing()
    {
        _repository.FailOnSave[1] = new InvalidOperationException("Database unavailable.");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UploadAsync(Command(), CancellationToken.None));

        Assert.Equal(["store.save", "repository.save(Uploaded)", "store.delete"], _log.Calls);
        Assert.Empty(_publisher.Published);
        Assert.Single(_store.Deleted);
    }

    [Fact]
    public async Task FailedPublishLeavesJobUploadedAndKeepsFile()
    {
        _publisher.FailWith = new InvalidOperationException("Broker unavailable.");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UploadAsync(Command(), CancellationToken.None));

        Assert.Equal(["store.save", "repository.save(Uploaded)", "publish"], _log.Calls);
        Assert.Equal(ProcessingStatus.Uploaded, _repository.Job!.Status);
        Assert.Empty(_store.Deleted);
    }

    [Fact]
    public async Task ConcurrencyConflictWhenMarkingQueuedIsTreatedAsSuccess()
    {
        _repository.FailOnSave[2] = new ConcurrencyConflictException();

        UploadPolicyResult result = await _service.UploadAsync(Command(), CancellationToken.None);

        Assert.Equal(_repository.Policy!.Id, result.PolicyId);
        Assert.Single(_publisher.Published);
    }

    private static UploadPolicyCommand Command() =>
        new(
            OrganisationId,
            "Current policy",
            BenefitType.PrivateMedicalInsurance,
            "CurrentHealthPolicy.pdf",
            new MemoryStream([0x25, 0x50, 0x44, 0x46, 0x2D]),
            1234);
}
