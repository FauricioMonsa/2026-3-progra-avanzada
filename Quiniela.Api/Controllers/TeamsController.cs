using Microsoft.AspNetCore.Mvc;
using Quiniela.BusinessLogic;
using Quiniela.Model;

namespace Quiniela.Api.Controllers;

[Route("api/teams")]
public sealed class TeamsController : CrudController<Team>
{
    public TeamsController(TeamCrudService crudService) : base(crudService) { }

    [HttpGet("{teamId:int}")]
    public Task<ActionResult<Team>> GetById(int teamId, CancellationToken cancellationToken) =>
        GetByKeyAsync(new object[] { teamId }, cancellationToken);

    [HttpPost]
    public Task<ActionResult<Team>> Create(Team team, CancellationToken cancellationToken) =>
        CreateAsync(team, nameof(GetById), new { teamId = team.TeamId }, cancellationToken);

    [HttpPut("{teamId:int}")]
    public Task<IActionResult> Update(int teamId, Team team, CancellationToken cancellationToken) =>
        UpdateAsync(
            new object[] { teamId },
            new object[] { team.TeamId },
            team,
            cancellationToken);

    [HttpDelete("{teamId:int}")]
    public Task<IActionResult> Delete(int teamId, CancellationToken cancellationToken) =>
        DeleteAsync(new object[] { teamId }, cancellationToken);
}