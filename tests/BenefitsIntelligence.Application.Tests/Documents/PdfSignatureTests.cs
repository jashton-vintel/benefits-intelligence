using System.Text;

using BenefitsIntelligence.Application.Documents;

namespace BenefitsIntelligence.Application.Tests.Documents;

public class PdfSignatureTests
{
    [Theory]
    [InlineData("%PDF-1.7\n...", true)]
    [InlineData("%PDF-", true)]
    [InlineData("%PDF", false)]
    [InlineData("Hello, world", false)]
    [InlineData("", false)]
    public async Task RecognisesPdfHeader(string content, bool expected)
    {
        using MemoryStream stream = new(Encoding.ASCII.GetBytes(content));

        Assert.Equal(expected, await PdfSignature.MatchesAsync(stream, CancellationToken.None));
    }

    [Fact]
    public async Task RewindsStreamAfterCheck()
    {
        using MemoryStream stream = new(Encoding.ASCII.GetBytes("%PDF-1.7"));

        await PdfSignature.MatchesAsync(stream, CancellationToken.None);

        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public async Task RejectsNonSeekableStream()
    {
        using MemoryStream inner = new(Encoding.ASCII.GetBytes("%PDF-1.7"));
        using BufferedStream forwardOnly = new(new NonSeekableStream(inner));

        await Assert.ThrowsAsync<ArgumentException>(() => PdfSignature.MatchesAsync(forwardOnly, CancellationToken.None));
    }

    private sealed class NonSeekableStream(Stream inner) : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
