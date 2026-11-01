using Microsoft.EntityFrameworkCore;
using Quiniela.Model;

namespace Quiniela.BusinessLogic;

public sealed class MatchCrudService : CrudService<Match>, IMatchQueryService
{
    private readonly QuinielaDbContext context;
    private readonly MatchQueryService queryService;

    public MatchCrudService(QuinielaDbContext context) : base(context)
    {
        this.context = context;
        queryService = new MatchQueryService(context);
    }

    public Task<(List<MatchDetails> Matches, int TotalCount)> GetMatchDetailsPageAsync(
        int? tournamentId,
        int? teamId,
        int pageNumber,
        int pageSize,
        int? matchId = null,
        CancellationToken cancellationToken = default) =>
        queryService.GetMatchDetailsPageAsync(
            tournamentId, teamId, pageNumber, pageSize, matchId, cancellationToken);

    public Task<string?> GetMatchStatsAsync(
        int matchId,
        CancellationToken cancellationToken = default) =>
        context.Matches.AsNoTracking()
            .Where(match => match.MatchId == matchId)
            .Select(match => match.Stats)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> DeleteWithDependentsAsync(
        int matchId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        Match? match = await context.Matches.FindAsync([matchId], cancellationToken);
        if (match is null)
        {
            return false;
        }

        List<Forecast> forecasts = await context.Forecasts
            .Where(forecast => forecast.MatchId == matchId)
            .ToListAsync(cancellationToken);
        List<MatchParticipant> participants = await context.MatchParticipants
            .Where(participant => participant.MatchId == matchId)
            .ToListAsync(cancellationToken);

        context.Forecasts.RemoveRange(forecasts);
        context.MatchParticipants.RemoveRange(participants);
        context.Matches.Remove(match);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task<List<MatchDetails>> GetMatchesByTeamAsync(
        int teamId,
        DateTime startDate,
        DateTime endDate,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default) =>
        queryService.GetMatchesByTeamAsync(teamId, startDate, endDate, pageNumber, pageSize, cancellationToken);

    public Task<List<MatchDetails>> GetMatchesByTeamAsync(
        int teamId,
        DateOnly startDate,
        DateOnly endDate,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default) =>
        queryService.GetMatchesByTeamAsync(teamId, startDate, endDate, pageNumber, pageSize, cancellationToken);

    public Task<List<MatchDetails>> GetMatchesByTournamentAsync(
        int tournamentId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default) =>
        queryService.GetMatchesByTournamentAsync(tournamentId, pageNumber, pageSize, cancellationToken);

    public List<MatchDetails> GetMatchesByTeam(int teamId, DateTime startDate, DateTime endDate) =>
        queryService.GetMatchesByTeam(teamId, startDate, endDate);

    public List<MatchDetails> GetMatchesByTournament(int tournamentId) =>
        queryService.GetMatchesByTournament(tournamentId);
}