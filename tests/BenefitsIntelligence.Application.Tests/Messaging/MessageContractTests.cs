using System.Text.Json;
using BenefitsIntelligence.Application.Comparison;
using BenefitsIntelligence.Application.Messaging;
using BenefitsIntelligence.Domain.Comparison;

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
        Assert.Equal(new DocumentSummary(PageCount: 5, ChunkCount: 6), message.Document);
        Assert.Equal([1, 2, 3, 4, 5], message.Pages.Select(p => p.PageNumber));
        Assert.Contains("NorthStar Health plc", message.Pages[0].Text);
    }

    [Fact]
    public void PolicyExtractionReadsSharedFixture()
    {
        PolicyExtraction extraction = Deserialize<ProcessPolicyCompleted>("process_completed.json").Extraction;

        Assert.Equal(PolicyExtraction.SupportedSchemaVersion, extraction.SchemaVersion);
        Assert.Equal("private_medical", extraction.BenefitType);
        Assert.Equal("NorthStar Health", extraction.Provider.Value);
        Assert.Equal(108000.00m, extraction.AnnualPremium.Value);
        Assert.Equal(new FactEvidence(3, 3, "An excess of £150 applies to each covered person once in each scheme year"), extraction.AnnualExcess.Evidence);
        Assert.Equal(new DateOnly(2027, 4, 1), extraction.EffectiveDate.Value);
        Assert.Null(extraction.DependantsIncluded.Value);
        Assert.Equal(0.4, extraction.DependantsIncluded.Confidence);
        Assert.Equal([FactIssue.Ambiguous], extraction.DependantsIncluded.Issues);
        Assert.Equal(new CoverageTerms(true, "10 sessions per scheme year with GP or specialist referral; 6 if self-referred", 10), extraction.Coverage.Physiotherapy.Value);
        Assert.Equal(0, extraction.Eligibility.MinimumServiceMonths.Value);
        Assert.Null(extraction.Eligibility.MinimumGrade.Value);
        Assert.NotNull(extraction.Eligibility.MinimumGrade.Evidence);
        Assert.Empty(extraction.Eligibility.MinimumGrade.Issues);
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

    [Fact]
    public void ComparisonSummaryRequestReadsSharedFixture()
    {
        ComparisonSummaryRequest request = Deserialize<ComparisonSummaryRequest>("comparison_summary_request.json");

        Assert.Equal("NorthStar Health", request.Proposed.Provider);
        Assert.Equal(
            new FieldDifference("annual_premium", ValueKind.Money, "120000.00", "108000.00", -12000m, Change.Decreased, false),
            request.Differences.Single(d => d.Field == "annual_premium"));
        Assert.Equal(Change.Removed, request.Differences.Single(d => d.Field == "eligibility.minimum_grade").Change);
        Assert.True(request.Differences.Single(d => d.Field == "dependants_included").NeedsReview);
    }

    // Compares property names rather than raw JSON: .NET writes UTC offsets as "+00:00" where the
    // fixture uses "Z", which both sides accept. Value fidelity is covered by the read tests above.
    [Theory]
    [InlineData("process_requested.json", typeof(ProcessPolicyRequested))]
    [InlineData("process_completed.json", typeof(ProcessPolicyCompleted))]
    [InlineData("process_failed.json", typeof(ProcessPolicyFailed))]
    [InlineData("comparison_summary_request.json", typeof(ComparisonSummaryRequest))]
    public void SerializedPropertiesMatchSharedFixture(string fixture, Type messageType)
    {
        var json = ReadFixture(fixture);

        var message = JsonSerializer.Deserialize(json, messageType, MessageSerialization.Options);
        var serialized = JsonSerializer.Serialize(message, messageType, MessageSerialization.Options);

        Assert.Equal(PropertyNames(json), PropertyNames(serialized));
    }

    private static T Deserialize<T>(string fixture) => ContractFixtures.Deserialize<T>(fixture);

    private static string ReadFixture(string name) => ContractFixtures.Read(name);

    // Paths of every property, nested ones included, so a renamed field anywhere fails the test.
    private static SortedSet<string> PropertyNames(string json)
    {
        using var document = JsonDocument.Parse(json);
        SortedSet<string> names = new(StringComparer.Ordinal);
        AddPropertyNames(document.RootElement, prefix: "", names);
        return names;
    }

    private static void AddPropertyNames(JsonElement element, string prefix, SortedSet<string> names)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    string path = prefix + property.Name;
                    names.Add(path);
                    AddPropertyNames(property.Value, path + ".", names);
                }

                break;

            case JsonValueKind.Array:
                foreach (JsonElement item in element.EnumerateArray())
                {
                    AddPropertyNames(item, prefix + "[].", names);
                }

                break;
        }
    }
}
