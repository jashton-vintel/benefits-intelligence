namespace BenefitsIntelligence.Domain.Policies;

/// <summary>
/// Where in the source document a fact was read from. Pages are found by locating the quote in
/// the document text, not reported by the model.
/// </summary>
public sealed record Evidence
{
    public const int MaxQuoteLength = 2000;

    public Evidence(int pageStart, int pageEnd, string quote)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageStart, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageEnd, pageStart);
        ArgumentException.ThrowIfNullOrWhiteSpace(quote);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quote.Length, MaxQuoteLength, nameof(quote));

        PageStart = pageStart;
        PageEnd = pageEnd;
        Quote = quote;
    }

    public int PageStart { get; }

    public int PageEnd { get; }

    public string Quote { get; }
}
