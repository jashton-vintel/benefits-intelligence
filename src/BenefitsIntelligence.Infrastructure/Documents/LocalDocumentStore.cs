using BenefitsIntelligence.Application.Documents;

namespace BenefitsIntelligence.Infrastructure.Documents;

internal sealed class LocalDocumentStore(string rootPath) : IDocumentStore
{
    public async Task<string> SaveAsync(Guid organisationId, Guid documentId, Stream content, CancellationToken cancellationToken)
    {
        // Paths are built only from identifiers, never from the uploaded file name, so callers
        // cannot write outside the root. Forward slashes keep stored paths portable.
        string storagePath = $"documents/{organisationId:N}/{documentId:N}.pdf";
        string fullPath = Resolve(storagePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        FileStream file = new(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);
        try
        {
            await using (file)
            {
                await content.CopyToAsync(file, cancellationToken);
            }
        }
        catch
        {
            File.Delete(fullPath);
            throw;
        }

        return storagePath;
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken)
    {
        File.Delete(Resolve(storagePath));
        return Task.CompletedTask;
    }

    private string Resolve(string storagePath) =>
        Path.Combine(rootPath, storagePath.Replace('/', Path.DirectorySeparatorChar));
}
