using BenefitsIntelligence.Domain.Policies;

namespace BenefitsIntelligence.Domain.Tests.Policies;

public class PolicyDocumentTests
{
    [Fact]
    public void RecordsPagesInPageOrder()
    {
        PolicyDocument document = NewDocument();

        document.RecordPages([new DocumentPage(2, "Second"), new DocumentPage(1, "First")]);

        Assert.Equal(["First", "Second"], document.Pages.Select(p => p.Text));
    }

    [Fact]
    public void RecordingAgainReplacesThePages()
    {
        PolicyDocument document = NewDocument();
        document.RecordPages([new DocumentPage(1, "First"), new DocumentPage(2, "Second")]);

        document.RecordPages([new DocumentPage(1, "Only")]);

        Assert.Equal(["Only"], document.Pages.Select(p => p.Text));
    }

    [Theory]
    [InlineData(new int[0])]
    [InlineData(new[] { 2, 3 })]
    [InlineData(new[] { 1, 1 })]
    [InlineData(new[] { 1, 3 })]
    public void PagesMustBeNumberedConsecutivelyFromOne(int[] numbers)
    {
        PolicyDocument document = NewDocument();

        Assert.Throws<ArgumentException>(() => document.RecordPages(numbers.Select(n => new DocumentPage(n, "Text"))));
        Assert.Empty(document.Pages);
    }

    private static PolicyDocument NewDocument() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "policy.pdf", "documents/policy.pdf", "application/pdf", 1024, DateTimeOffset.UtcNow);
}
