namespace Quiniela.BusinessLogic;

public interface IForecastQueryService
{
    Task<List<ForecastCountByHour>> GetCountsByHourAsync(
        int matchId,
        CancellationToken cancellationToken = default);
}
