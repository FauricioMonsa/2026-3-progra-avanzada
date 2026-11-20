using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Quiniela.Model;

namespace Quiniela.BusinessLogic;

public sealed class ForecastQueryService : IForecastQueryService
{
    private readonly QuinielaDbContext context;

    public ForecastQueryService(QuinielaDbContext context)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<ForecastCountByHour>> GetCountsByHourAsync(
        int matchId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(matchId, 1);

        string? timeZoneId = await context.Matches.AsNoTracking()
            .Where(match => match.MatchId == matchId)
            .Select(match => (string?)match.TimeZone)
            .SingleOrDefaultAsync(cancellationToken);
        if (timeZoneId is null)
        {
            return [];
        }

        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var forecasts = await context.Forecasts.AsNoTracking()
            .Where(forecast => forecast.MatchId == matchId)
            .Select(forecast => new
            {
                forecast.SubmittedAt,
                forecast.HomeScore,
                forecast.AwayScore
            })
            .ToListAsync(cancellationToken);

        return forecasts
            .GroupBy(forecast =>
            {
                DateTime submittedAtUtc = DateTime.SpecifyKind(forecast.SubmittedAt, DateTimeKind.Utc);
                DateTime submittedAtLocal = TimeZoneInfo.ConvertTimeFromUtc(submittedAtUtc, timeZone);
                int resultType = forecast.HomeScore.CompareTo(forecast.AwayScore);
                return new
                {
                    Date = DateOnly.FromDateTime(submittedAtLocal),
                    Hour = submittedAtLocal.Hour,
                    ResultType = resultType
                };
            })
            .OrderBy(group => group.Key.Date)
            .ThenBy(group => group.Key.Hour)
            .ThenBy(group => group.Key.ResultType)
            .Select(group => new ForecastCountByHour(
                group.Key.Date.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture),
                group.Key.Hour.ToString("D2", CultureInfo.InvariantCulture),
                group.Key.ResultType,
                group.Count()))
            .ToList();
    }
}
