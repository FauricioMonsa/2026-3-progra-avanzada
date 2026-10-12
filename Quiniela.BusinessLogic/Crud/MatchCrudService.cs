using Quiniela.Model;

namespace Quiniela.BusinessLogic;

public sealed class MatchCrudService : CrudService<Match>
{
    public MatchCrudService(QuinielaDbContext context) : base(context) { }
}