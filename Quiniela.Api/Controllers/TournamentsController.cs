using Microsoft.AspNetCore.Mvc;
using Quiniela.BusinessLogic;
using Quiniela.Model;

namespace Quiniela.Api.Controllers;

[Route("api/tournaments")]
public sealed class TournamentsController : CrudController<Tournament>
{
    public TournamentsController(TournamentCrudService crudService) : base(crudService) { }

    [HttpGet("{tournamentId:int}")]
    public Task<ActionResult<Tournament>> GetById(int tournamentId, CancellationToken cancellationToken) =>
        GetByKeyAsync(new object[] { tournamentId }, cancellationToken);

    [HttpPost]
    public Task<ActionResult<Tournament>> Create(Tournament tournament, CancellationToken cancellationToken) =>
        CreateAsync(tournament, nameof(GetById), new { tournamentId = tournament.TournamentId }, cancellationToken);

    [HttpPut("{tournamentId:int}")]
    public Task<IActionResult> Update(
        int tournamentId,
        Tournament tournament,
        CancellationToken cancellationToken) =>
        UpdateAsync(
            new object[] { tournamentId },
            new object[] { tournament.TournamentId },
            tournament,
            cancellationToken);

    [HttpDelete("{tournamentId:int}")]
    public Task<IActionResult> Delete(int tournamentId, CancellationToken cancellationToken) =>
        DeleteAsync(new object[] { tournamentId }, cancellationToken);
}