using System.Text.Json;
using BenefitsIntelligence.Application.Messaging;

namespace BenefitsIntelligence.Application.Tests.Messaging;

public class MessageContractTests
{
    private static readonly Guid FixtureCorrelationId = Guid.Parse("a3d9e5f1-7c2b-4e6a-8f10-2b4c6d8e0f12");
    private static readonly Guid FixtureTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FixturePolicyId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void ProcessPolicyRequestedReadsSharedFixture()
    {
        var message = Deserialize<ProcessPolicyRequested>("process_requested.json");

        Assert.Equal(Guid.Parse("6f1c2a9e-0b4d-4c8e-9a51-3d2e7f8b1c40"), message.MessageId);
        Assert.Equal(FixtureCorrelationId, message.CorrelationId);
        Assert.Equal(MessageSerialization.SchemaVersion, message.SchemaVersion);
        Assert.Equal(FixtureTenantId, message.TenantId);
        Assert.Equal(FixturePolicyId, message.PolicyId);
        Assert.Equal(Guid.Parse("33333333-3333-3333-3333-333333333333"), message.DocumentId);
        Assert.Equal("documents/22222222-2222-2222-2222-222222222222.pdf", message.DocumentLocation);
        Assert.Equal(new DateTimeOffset(2026, 10, 2, 8, 0, 0, TimeSpan.Zero), message.RequestedAt);
    }

    [Fact]
    public void ProcessPolicyCompletedReadsSharedFixture()
    {
        var message = Deserialize<ProcessPolicyCompleted>("process_completed.json");

        Assert.Equal(Guid.Parse("9b8e7d6c-5a4b-4c3d-8e2f-1a0b9c8d7e6f"), message.MessageId);
        Assert.Equal(FixtureCorrelationId, message.CorrelationId);
        Assert.Equal(MessageSerialization.SchemaVersion, message.SchemaVersion);
        Assert.Equal(FixtureTenantId, message.TenantId);
        Assert.Equal(FixturePolicyId, message.PolicyId);
        Assert.Equal("completed", message.Status);
        Assert.Equal(new DocumentSummary(PageCount: 7, ChunkCount: 8), message.Document);
        Assert.Equal(new PolicyExtraction("Atlas Healthcare", "Corporate Plus", 100.00m), message.Extraction);
    }

    [Fact]
    public void ProcessPolicyFailedReadsSharedFixture()
    {
        var message = Deserialize<ProcessPolicyFailed>("process_failed.json");

        Assert.Equal(Guid.Parse("4c2e8f1a-6b3d-4e9f-a1c7-5d8b2f0e6a93"), message.MessageId);
        Assert.Equal(FixtureCorrelationId, message.CorrelationId);
        Assert.Equal(MessageSerialization.SchemaVersion, message.SchemaVersion);
        Assert.Equal(FixtureTenantId, message.TenantId);
        Assert.Equal(FixturePolicyId, message.PolicyId);
        Assert.Equal("failed", message.Status);
        Assert.Equal("INVALID_DOCUMENT", message.ErrorCode);
        Assert.Equal("The PDF contains no extractable text.", message.ErrorMessage);
    }

    // Compares property names rather than raw JSON: .NET writes UTC offsets as "+00:00" where the
    // fixture uses "Z", which both sides accept. Value fidelity is covered by the read tests above.
    [Theory]
    [InlineData("process_requested.json", typeof(ProcessPolicyRequested))]
    [InlineData("process_completed.json", typeof(ProcessPolicyCompleted))]
    [InlineData("process_failed.json", typeof(ProcessPolicyFailed))]
    public void SerializedPropertiesMatchSharedFixture(string fixture, Type messageType)
    {
        var json = ReadFixture(fixture);

        var message = JsonSerializer.Deserialize(json, messageType, MessageSerialization.Options);
        var serialized = JsonSerializer.Serialize(message, messageType, MessageSerialization.Options);

        Assert.Equal(PropertyNames(json), PropertyNames(serialized));
    }

    private static T Deserialize<T>(string fixture)
    {
        var message = JsonSerializer.Deserialize<T>(ReadFixture(fixture), MessageSerialization.Options);
        Assert.NotNull(message);
        return message;
    }

    private static string ReadFixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", name));

    private static SortedSet<string> PropertyNames(string json)
    {
        using var document = JsonDocument.Parse(json);
        return new SortedSet<string>(
            document.RootElement.EnumerateObject().Select(property => property.Name),
            StringComparer.Ordinal);
    }
}
