namespace BenefitsIntelligence.Infrastructure.Documents;

public sealed class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";

    /// <summary>
    /// Absolute, or relative to the application's content root. Must point at the same
    /// location the processing worker reads from.
    /// </summary>
    public string RootPath { get; set; } = string.Empty;
}
