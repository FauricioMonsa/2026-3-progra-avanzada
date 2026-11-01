using Microsoft.AspNetCore.Mvc;
using Quiniela.BusinessLogic;
using Quiniela.Model;

namespace Quiniela.Api.Controllers;

[Route("api/matches")]
public sealed class MatchesController : CrudController<Match>
{
    private readonly MatchCrudService matchService;

    public MatchesController(MatchCrudService crudService) : base(crudService)
    {
        matchService = crudService;
    }

    [HttpGet("{matchId:int}")]
    public Task<ActionResult<Match>> GetById(int matchId, CancellationToken cancellationToken) =>
        GetByKeyAsync(new object[] { matchId }, cancellationToken);

    [HttpPost]
    public Task<ActionResult<Match>> Create(Match match, CancellationToken cancellationToken) =>
        CreateAsync(match, nameof(GetById), new { matchId = match.MatchId }, cancellationToken);

    [HttpPut("{matchId:int}")]
    public Task<IActionResult> Update(int matchId, Match match, CancellationToken cancellationToken) =>
        UpdateAsync(
            new object[] { matchId },
            new object[] { match.MatchId },
            match,
            cancellationToken);

    [HttpDelete("{matchId:int}")]
    public Task<IActionResult> Delete(int matchId, CancellationToken cancellationToken) =>
        DeleteAsync(new object[] { matchId }, cancellationToken);

    [HttpGet("by-team/{teamId:int}")]
    public async Task<ActionResult<List<MatchDetails>>> GetByTeam(
        int teamId,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await matchService.GetMatchesByTeamAsync(
                teamId, startDate, endDate, pageNumber, pageSize, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("by-tournament/{tournamentId:int}")]
    public async Task<ActionResult<List<MatchDetails>>> GetByTournament(
        int tournamentId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await matchService.GetMatchesByTournamentAsync(
                tournamentId, pageNumber, pageSize, cancellationToken));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}