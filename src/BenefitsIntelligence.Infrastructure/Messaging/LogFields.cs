using System.Collections;

namespace BenefitsIntelligence.Infrastructure.Messaging;

/// <summary>
/// Structured fields for a logging scope. Formatters write each field as a property; the text
/// form keeps the scope readable where it is shown as a message.
/// </summary>
internal sealed class LogFields(params KeyValuePair<string, object?>[] fields) : IReadOnlyList<KeyValuePair<string, object?>>
{
    public int Count => fields.Length;

    public KeyValuePair<string, object?> this[int index] => fields[index];

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => ((IEnumerable<KeyValuePair<string, object?>>)fields).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => string.Join(", ", fields.Select(field => $"{field.Key}={field.Value}"));
}
