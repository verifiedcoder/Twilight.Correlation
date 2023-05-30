using RT.Comb;

namespace Twilight.Correlation;

public sealed class CorrelationProvider : ICorrelationProvider
{
    private static readonly AsyncLocal<string> Identifier = new();

    public CorrelationProvider(ICombProvider combProvider) => Identifier.Value = combProvider.Create().ToString();

    public AsyncLocal<string> CorrelationId => Identifier;
}
