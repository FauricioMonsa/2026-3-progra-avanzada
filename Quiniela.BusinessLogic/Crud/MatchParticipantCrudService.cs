using Quiniela.Model;

namespace Quiniela.BusinessLogic;

public sealed class MatchParticipantCrudService : CrudService<MatchParticipant>
{
    public MatchParticipantCrudService(QuinielaDbContext context) : base(context) { }
}