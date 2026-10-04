namespace BenefitsIntelligence.Domain.Policies;

/// <summary>The text of one page, kept so questions can be answered and cited from the document.</summary>
public sealed class DocumentPage
{
    public DocumentPage(int pageNumber, string text)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentNullException.ThrowIfNull(text);

        Id = Guid.NewGuid();
        PageNumber = pageNumber;
        Text = text;
    }

    private DocumentPage()
    {
        Text = null!;
    }

    public Guid Id { get; private set; }

    public int PageNumber { get; private set; }

    public string Text { get; private set; }
}
