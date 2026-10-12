using Microsoft.EntityFrameworkCore;
using Quiniela.Model;

namespace Quiniela.BusinessLogic;

public class MatchQueryService : IMatchQueryService
{
    private readonly QuinielaDbContext context;

    public MatchQueryService(QuinielaDbContext context)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Query 1: List of all matches a team has played on a time interval, sorted chronologically ascending.
    /// </summary>
    public async Task<List<MatchDetails>> GetMatchesByTeamAsync(
        int teamId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (startDate > endDate)
        {
            throw new ArgumentException("The start date must be less than or equal to the end date.", nameof(startDate));
        }

        var rawList = await (
            from match in context.Matches.AsNoTracking()
            where (match.HomeTeamId == teamId || match.AwayTeamId == teamId)
               && match.KickoffAt >= startDate
               && match.KickoffAt <= endDate
            join homeTeam in context.Teams.AsNoTracking() on match.HomeTeamId equals homeTeam.TeamId
            join awayTeam in context.Teams.AsNoTracking() on match.AwayTeamId equals awayTeam.TeamId
            join tournament in context.Tournaments.AsNoTracking() on match.TournamentId equals (int?)tournament.TournamentId into tournamentGroup
            from tournament in tournamentGroup.DefaultIfEmpty()
            orderby match.KickoffAt, match.MatchId
            select new
            {
                TournamentName = tournament != null ? tournament.Name : null,
                TournamentSeason = tournament != null ? (short?)tournament.SeasonYear : null,
                TournamentSport = tournament != null ? (SportType?)tournament.Sport : null,
                match.MatchId,
                match.Kind,
                HomeTeamName = homeTeam.Name,
                AwayTeamName = awayTeam.Name,
                match.KickoffAt,
                match.Status,
                match.HomeScore,
                match.AwayScore
            }
        ).ToListAsync(cancellationToken);

        return rawList.Select(m => new MatchDetails(
            m.TournamentName,
            m.TournamentSeason,
            m.TournamentSport?.ToString(),
            m.MatchId,
            m.Kind,
            m.HomeTeamName,
            m.AwayTeamName,
            m.KickoffAt,
            m.Status.ToString(),
            m.HomeScore,
            m.AwayScore
        )).ToList();
    }

    /// <summary>
    /// Query 1 (DateOnly overload): List of all matches a team has played on a date interval, sorted chronologically ascending.
    /// </summary>
    public async Task<List<MatchDetails>> GetMatchesByTeamAsync(
        int teamId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        if (startDate > endDate)
        {
            throw new ArgumentException("The start date must be less than or equal to the end date.", nameof(startDate));
        }

        var rawList = await (
            from match in context.Matches.AsNoTracking()
            where (match.HomeTeamId == teamId || match.AwayTeamId == teamId)
               && match.MatchDay >= startDate
               && match.MatchDay <= endDate
            join homeTeam in context.Teams.AsNoTracking() on match.HomeTeamId equals homeTeam.TeamId
            join awayTeam in context.Teams.AsNoTracking() on match.AwayTeamId equals awayTeam.TeamId
            join tournament in context.Tournaments.AsNoTracking() on match.TournamentId equals (int?)tournament.TournamentId into tournamentGroup
            from tournament in tournamentGroup.DefaultIfEmpty()
            orderby match.KickoffAt, match.MatchId
            select new
            {
                TournamentName = tournament != null ? tournament.Name : null,
                TournamentSeason = tournament != null ? (short?)tournament.SeasonYear : null,
                TournamentSport = tournament != null ? (SportType?)tournament.Sport : null,
                match.MatchId,
                match.Kind,
                HomeTeamName = homeTeam.Name,
                AwayTeamName = awayTeam.Name,
                match.KickoffAt,
                match.Status,
                match.HomeScore,
                match.AwayScore
            }
        ).ToListAsync(cancellationToken);

        return rawList.Select(m => new MatchDetails(
            m.TournamentName,
            m.TournamentSeason,
            m.TournamentSport?.ToString(),
            m.MatchId,
            m.Kind,
            m.HomeTeamName,
            m.AwayTeamName,
            m.KickoffAt,
            m.Status.ToString(),
            m.HomeScore,
            m.AwayScore
        )).ToList();
    }

    /// <summary>
    /// Query 2: List of all matches of a tournament, sorted chronologically ascending.
    /// </summary>
    public async Task<List<MatchDetails>> GetMatchesByTournamentAsync(
        int tournamentId,
        CancellationToken cancellationToken = default)
    {
        var rawList = await (
            from match in context.Matches.AsNoTracking()
            where match.TournamentId == tournamentId
            join tournament in context.Tournaments.AsNoTracking() on match.TournamentId equals (int?)tournament.TournamentId
            join homeTeam in context.Teams.AsNoTracking() on match.HomeTeamId equals homeTeam.TeamId
            join awayTeam in context.Teams.AsNoTracking() on match.AwayTeamId equals awayTeam.TeamId
            orderby match.KickoffAt, match.MatchId
            select new
            {
                TournamentName = tournament.Name,
                TournamentSeason = (short?)tournament.SeasonYear,
                TournamentSport = (SportType?)tournament.Sport,
                match.MatchId,
                match.Kind,
                HomeTeamName = homeTeam.Name,
                AwayTeamName = awayTeam.Name,
                match.KickoffAt,
                match.Status,
                match.HomeScore,
                match.AwayScore
            }
        ).ToListAsync(cancellationToken);

        return rawList.Select(m => new MatchDetails(
            m.TournamentName,
            m.TournamentSeason,
            m.TournamentSport?.ToString(),
            m.MatchId,
            m.Kind,
            m.HomeTeamName,
            m.AwayTeamName,
            m.KickoffAt,
            m.Status.ToString(),
            m.HomeScore,
            m.AwayScore
        )).ToList();
    }

    // Synchronous and alias convenience methods
    public List<MatchDetails> GetMatchesByTeam(int teamId, DateTime startDate, DateTime endDate) =>
        GetMatchesByTeamAsync(teamId, startDate, endDate).GetAwaiter().GetResult();

    public List<MatchDetails> GetMatchesByTournament(int tournamentId) =>
        GetMatchesByTournamentAsync(tournamentId).GetAwaiter().GetResult();

    public Task<List<MatchDetails>> GetTeamMatchesAsync(int teamId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) =>
        GetMatchesByTeamAsync(teamId, startDate, endDate, cancellationToken);

    public Task<List<MatchDetails>> GetTournamentMatchesAsync(int tournamentId, CancellationToken cancellationToken = default) =>
        GetMatchesByTournamentAsync(tournamentId, cancellationToken);
}
