namespace BenefitsIntelligence.Application.Documents;

public static class PdfSignature
{
    private const int HeaderLength = 5;

    /// <summary>
    /// Checks the leading "%PDF-" marker rather than trusting the file name or the
    /// client-supplied content type. Leaves the stream positioned at the start.
    /// </summary>
    public static async Task<bool> MatchesAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanSeek)
        {
            throw new ArgumentException("Stream must be seekable.", nameof(stream));
        }

        byte[] header = new byte[HeaderLength];
        int read = await stream.ReadAtLeastAsync(header, HeaderLength, throwOnEndOfStream: false, cancellationToken);
        stream.Position = 0;

        return read == HeaderLength && header.AsSpan().SequenceEqual("%PDF-"u8);
    }
}
