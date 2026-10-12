using Quiniela.Model;

namespace Quiniela.BusinessLogic;

public sealed class TournamentCrudService : CrudService<Tournament>
{
    public TournamentCrudService(QuinielaDbContext context) : base(context) { }
}