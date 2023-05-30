namespace Twilight.Correlation;

public interface ICorrelationProvider
{
    AsyncLocal<string> CorrelationId { get; }
}
