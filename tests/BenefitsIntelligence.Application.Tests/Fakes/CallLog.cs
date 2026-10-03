namespace BenefitsIntelligence.Application.Tests.Fakes;

internal sealed class CallLog
{
    private readonly List<string> _calls = [];

    public IReadOnlyList<string> Calls => _calls;

    public void Record(string call) => _calls.Add(call);
}
