using Quiniela.Model;

namespace Quiniela.BusinessLogic;

public sealed class UserCrudService : CrudService<User>
{
    public UserCrudService(QuinielaDbContext context) : base(context) { }
}