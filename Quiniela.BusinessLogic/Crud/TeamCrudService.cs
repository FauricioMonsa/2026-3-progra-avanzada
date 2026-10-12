using Quiniela.Model;

namespace Quiniela.BusinessLogic;

public sealed class TeamCrudService : CrudService<Team>
{
    public TeamCrudService(QuinielaDbContext context) : base(context) { }
}