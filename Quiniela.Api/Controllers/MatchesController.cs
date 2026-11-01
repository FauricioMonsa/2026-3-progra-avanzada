using Microsoft.EntityFrameworkCore;
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

    [HttpGet("details")]
    public async Task<ActionResult<List<MatchDetails>>> GetDetails(
        [FromQuery] int? tournamentId,
        [FromQuery] int? teamId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1)
        {
            return BadRequest("Page number and page size must be greater than zero.");
        }

        try
        {
            var page = await matchService.GetMatchDetailsPageAsync(
                tournamentId, teamId, pageNumber, pageSize, cancellationToken: cancellationToken);
            Response.Headers["X-Total-Count"] = page.TotalCount.ToString();
            return Ok(page.Matches);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (OverflowException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpGet("details/{matchId:int}")]
    public async Task<ActionResult<MatchDetails>> GetDetailsById(
        int matchId,
        CancellationToken cancellationToken)
    {
        var page = await matchService.GetMatchDetailsPageAsync(
            null, null, 1, 1, matchId, cancellationToken);
        return page.Matches.Count == 0 ? NotFound() : Ok(page.Matches[0]);
    }

    [HttpGet("stats/{matchId:int}")]
    public async Task<IActionResult> GetStats(int matchId, CancellationToken cancellationToken)
    {
        string? stats = await matchService.GetMatchStatsAsync(matchId, cancellationToken);
        return stats is null
            ? NotFound()
            : Content(stats, "application/json; charset=utf-8");
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
    public async Task<IActionResult> Delete(int matchId, CancellationToken cancellationToken)
    {
        try
        {
            return await matchService.DeleteWithDependentsAsync(matchId, cancellationToken)
                ? NoContent()
                : NotFound();
        }
        catch (DbUpdateException)
        {
            return Conflict("The match could not be deleted because related data still references it.");
        }
    }

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