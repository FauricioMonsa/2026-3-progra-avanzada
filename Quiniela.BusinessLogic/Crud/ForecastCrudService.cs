using Microsoft.EntityFrameworkCore;
using Quiniela.Model;

namespace Quiniela.BusinessLogic;

public sealed class ForecastCrudService : CrudService<Forecast>
{
    private readonly QuinielaDbContext context;

    public ForecastCrudService(QuinielaDbContext context) : base(context)
    {
        this.context = context;
    }

    public Task<List<Forecast>> GetForMatchesAsync(
        IReadOnlyCollection<int> matchIds,
        CancellationToken cancellationToken = default)
    {
        if (matchIds.Count == 0)
        {
            return Task.FromResult(new List<Forecast>());
        }

        return context.Forecasts.AsNoTracking()
            .Where(forecast => matchIds.Contains(forecast.MatchId))
            .OrderBy(forecast => forecast.UserId)
            .ThenBy(forecast => forecast.SubmittedAt)
            .Select(forecast => new Forecast
            {
                ForecastId = forecast.ForecastId,
                MatchId = forecast.MatchId,
                UserId = forecast.UserId,
                HomeScore = forecast.HomeScore,
                AwayScore = forecast.AwayScore,
                SubmittedAt = forecast.SubmittedAt
            })
            .ToListAsync(cancellationToken);
    }
}