using Quiniela.Model;

namespace Quiniela.BusinessLogic;

public sealed class MatchCrudService : CrudService<Match>, IMatchQueryService
{
    private readonly MatchQueryService queryService;

    public MatchCrudService(QuinielaDbContext context) : base(context)
    {
        queryService = new MatchQueryService(context);
    }

    public Task<List<MatchDetails>> GetMatchesByTeamAsync(
        int teamId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default) =>
        queryService.GetMatchesByTeamAsync(teamId, startDate, endDate, cancellationToken);

    public Task<List<MatchDetails>> GetMatchesByTeamAsync(
        int teamId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default) =>
        queryService.GetMatchesByTeamAsync(teamId, startDate, endDate, cancellationToken);

    public Task<List<MatchDetails>> GetMatchesByTournamentAsync(
        int tournamentId,
        CancellationToken cancellationToken = default) =>
        queryService.GetMatchesByTournamentAsync(tournamentId, cancellationToken);

    public List<MatchDetails> GetMatchesByTeam(int teamId, DateTime startDate, DateTime endDate) =>
        queryService.GetMatchesByTeam(teamId, startDate, endDate);

    public List<MatchDetails> GetMatchesByTournament(int tournamentId) =>
        queryService.GetMatchesByTournament(tournamentId);
}