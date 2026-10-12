using Quiniela.Model;

namespace Quiniela.BusinessLogic;

public sealed class ForecastCrudService : CrudService<Forecast>
{
    public ForecastCrudService(QuinielaDbContext context) : base(context) { }
}