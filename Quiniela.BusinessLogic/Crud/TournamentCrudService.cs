using Quiniela.Model;

namespace Quiniela.BusinessLogic;

public sealed class TournamentCrudService : CrudService<Tournament>
{
    private readonly MatchQueryService queryService;

    public TournamentCrudService(QuinielaDbContext context) : base(context)
    {
        queryService = new MatchQueryService(context);
    }

    public Task<List<MatchDetails>> GetMatchesByTournamentAsync(
        int tournamentId,
        CancellationToken cancellationToken = default) =>
        queryService.GetMatchesByTournamentAsync(tournamentId, cancellationToken);
}