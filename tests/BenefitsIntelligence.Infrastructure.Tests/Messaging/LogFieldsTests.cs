using BenefitsIntelligence.Infrastructure.Messaging;

namespace BenefitsIntelligence.Infrastructure.Tests.Messaging;

public class LogFieldsTests
{
    [Fact]
    public void ExposesEachFieldForStructuredLogging()
    {
        LogFields fields = new(new("correlation_id", "a1"), new("message_id", "b2"));

        Assert.Equal(["correlation_id", "message_id"], fields.Select(field => field.Key));
        Assert.Equal("b2", fields[1].Value);
    }

    [Fact]
    public void ReadsAsTheFieldsWhenWrittenAsText()
    {
        LogFields fields = new(new("correlation_id", "a1"), new("message_id", null));

        Assert.Equal("correlation_id=a1, message_id=", fields.ToString());
    }
}
