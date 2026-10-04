using System.Text.Json;

using BenefitsIntelligence.Application.Messaging;

namespace BenefitsIntelligence.Application.Tests;

internal static class ContractFixtures
{
    public static string Read(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", name));

    public static T Deserialize<T>(string name)
    {
        T? message = JsonSerializer.Deserialize<T>(Read(name), MessageSerialization.Options);
        Assert.NotNull(message);
        return message;
    }
}
