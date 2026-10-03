using BenefitsIntelligence.Infrastructure.Documents;

namespace BenefitsIntelligence.Infrastructure.Tests.Documents;

public sealed class LocalDocumentStoreTests : IDisposable
{
    private static readonly Guid OrganisationId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly string _root = Path.Combine(Path.GetTempPath(), "bi-store-" + Guid.NewGuid().ToString("N"));
    private readonly LocalDocumentStore _store;

    public LocalDocumentStoreTests()
    {
        _store = new LocalDocumentStore(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task SaveWritesContentAndReturnsRelativeForwardSlashPath()
    {
        Guid documentId = Guid.NewGuid();
        byte[] content = [0x25, 0x50, 0x44, 0x46];

        string storagePath = await _store.SaveAsync(OrganisationId, documentId, new MemoryStream(content), CancellationToken.None);

        Assert.Equal($"documents/{OrganisationId:N}/{documentId:N}.pdf", storagePath);
        Assert.Equal(content, await File.ReadAllBytesAsync(FullPath(storagePath)));
    }

    [Fact]
    public async Task SaveDoesNotOverwriteExistingDocument()
    {
        Guid documentId = Guid.NewGuid();
        string storagePath = await _store.SaveAsync(OrganisationId, documentId, new MemoryStream([1, 2, 3]), CancellationToken.None);

        await Assert.ThrowsAsync<IOException>(
            () => _store.SaveAsync(OrganisationId, documentId, new MemoryStream([9, 9, 9]), CancellationToken.None));

        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(FullPath(storagePath)));
    }

    [Fact]
    public async Task FailedSaveLeavesNoPartialFile()
    {
        Guid documentId = Guid.NewGuid();

        await Assert.ThrowsAsync<IOException>(
            () => _store.SaveAsync(OrganisationId, documentId, new FailingStream(), CancellationToken.None));

        Assert.False(File.Exists(FullPath($"documents/{OrganisationId:N}/{documentId:N}.pdf")));
    }

    [Fact]
    public async Task DeleteRemovesDocumentAndToleratesMissingFile()
    {
        string storagePath = await _store.SaveAsync(OrganisationId, Guid.NewGuid(), new MemoryStream([1]), CancellationToken.None);

        await _store.DeleteAsync(storagePath, CancellationToken.None);
        await _store.DeleteAsync(storagePath, CancellationToken.None);

        Assert.False(File.Exists(FullPath(storagePath)));
    }

    private string FullPath(string storagePath) =>
        Path.Combine(_root, storagePath.Replace('/', Path.DirectorySeparatorChar));

    private sealed class FailingStream : MemoryStream
    {
        public FailingStream()
            : base([1, 2, 3])
        {
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
            throw new IOException("Simulated read failure.");
    }
}
