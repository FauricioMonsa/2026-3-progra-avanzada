using Microsoft.AspNetCore.Mvc;
using Quiniela.BusinessLogic;
using Quiniela.Model;

namespace Quiniela.Api.Controllers;

[Route("api/tournament-participants")]
public sealed class TournamentParticipantsController : CrudController<TournamentTeam>
{
    public TournamentParticipantsController(TournamentTeamCrudService crudService) : base(crudService) { }

    [HttpGet("{tournamentId:int}/{teamId:int}")]
    public Task<ActionResult<TournamentTeam>> GetById(
        int tournamentId,
        int teamId,
        CancellationToken cancellationToken) =>
        GetByKeyAsync(new object[] { tournamentId, teamId }, cancellationToken);

    [HttpPost]
    public Task<ActionResult<TournamentTeam>> Create(
        TournamentTeam participant,
        CancellationToken cancellationToken) =>
        CreateAsync(
            participant,
            nameof(GetById),
            new { participant.TournamentId, participant.TeamId },
            cancellationToken);

    [HttpPut("{tournamentId:int}/{teamId:int}")]
    public Task<IActionResult> Update(
        int tournamentId,
        int teamId,
        TournamentTeam participant,
        CancellationToken cancellationToken) =>
        UpdateAsync(
            new object[] { tournamentId, teamId },
            new object[] { participant.TournamentId, participant.TeamId },
            participant,
            cancellationToken);

    [HttpDelete("{tournamentId:int}/{teamId:int}")]
    public Task<IActionResult> Delete(
        int tournamentId,
        int teamId,
        CancellationToken cancellationToken) =>
        DeleteAsync(new object[] { tournamentId, teamId }, cancellationToken);
}