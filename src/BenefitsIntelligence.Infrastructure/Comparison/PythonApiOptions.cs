using System.Diagnostics.CodeAnalysis;

namespace BenefitsIntelligence.Infrastructure.Comparison;

public sealed class PythonApiOptions
{
    public const string SectionName = "PythonApi";

    public string? BaseUrl { get; set; }

    public string? ApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 30;

    [MemberNotNullWhen(true, nameof(BaseUrl), nameof(ApiKey))]
    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(ApiKey);
}
