using System.Text.Json;

namespace BenefitsIntelligence.Application.Messaging;

public static class MessageSerialization
{
    public const string SchemaVersion = "1.0";

    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };
}