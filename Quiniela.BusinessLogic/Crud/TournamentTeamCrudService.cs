using Quiniela.Model;

namespace Quiniela.BusinessLogic;

public sealed class TournamentTeamCrudService : CrudService<TournamentTeam>
{
    public TournamentTeamCrudService(QuinielaDbContext context) : base(context) { }
}