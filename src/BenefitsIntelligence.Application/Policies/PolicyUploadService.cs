using BenefitsIntelligence.Application.Documents;
using BenefitsIntelligence.Application.Messaging;
using BenefitsIntelligence.Application.Persistence;
using BenefitsIntelligence.Domain.Policies;
using BenefitsIntelligence.Domain.Processing;

namespace BenefitsIntelligence.Application.Policies;

public sealed class PolicyUploadService(
    IDocumentStore documentStore,
    IPolicyRepository repository,
    IProcessingRequestPublisher publisher,
    TimeProvider timeProvider)
{
    private const string PdfContentType = "application/pdf";

    public async Task<UploadPolicyResult> UploadAsync(UploadPolicyCommand command, CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        Guid documentId = Guid.NewGuid();
        Guid policyId = Guid.NewGuid();

        string storagePath = await documentStore.SaveAsync(command.OrganisationId, documentId, command.Content, cancellationToken);

        PolicyDocument document = new(documentId, command.OrganisationId, command.FileName, storagePath, PdfContentType, command.SizeBytes, now);
        BenefitPolicy policy = new(policyId, command.OrganisationId, command.Name, command.BenefitType, documentId, now);
        ProcessingJob job = ProcessingJob.Create(policyId, now);

        repository.Add(document, policy, job);

        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await documentStore.DeleteAsync(storagePath, CancellationToken.None);
            throw;
        }

        // The records are committed from here on, so finish dispatching even if the caller
        // disconnects. If publishing fails the job stays Uploaded, which is visible rather than silently lost
        await publisher.PublishAsync(
            new ProcessPolicyRequested(
                MessageId: Guid.NewGuid(),
                CorrelationId: job.CorrelationId,
                SchemaVersion: MessageSerialization.SchemaVersion,
                TenantId: command.OrganisationId,
                PolicyId: policyId,
                DocumentId: documentId,
                DocumentLocation: storagePath,
                RequestedAt: now),
            CancellationToken.None);

        if (job.MarkQueued(timeProvider.GetUtcNow()))
        {
            try
            {
                await repository.SaveChangesAsync(CancellationToken.None);
            }
            catch (ConcurrencyConflictException)
            {
                // The worker's result was recorded first.. that status supersedes Queued
            }
        }

        return new UploadPolicyResult(policyId, job.CorrelationId);
    }
}
