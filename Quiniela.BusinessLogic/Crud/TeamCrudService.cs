using Quiniela.Model;

namespace Quiniela.BusinessLogic;

public sealed class TeamCrudService : CrudService<Team>
{
    private readonly MatchQueryService queryService;

    public TeamCrudService(QuinielaDbContext context) : base(context)
    {
        queryService = new MatchQueryService(context);
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
}