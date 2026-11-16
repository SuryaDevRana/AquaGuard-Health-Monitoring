namespace AquaGuard.Application.EarlyWarnings;

public interface IEarlyWarningService
{
    Task<int> EvaluateAndCreateWarningsAsync(CancellationToken cancellationToken = default);
}
