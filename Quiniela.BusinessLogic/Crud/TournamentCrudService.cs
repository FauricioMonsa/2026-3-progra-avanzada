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
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default) =>
        queryService.GetMatchesByTournamentAsync(tournamentId, pageNumber, pageSize, cancellationToken);
}