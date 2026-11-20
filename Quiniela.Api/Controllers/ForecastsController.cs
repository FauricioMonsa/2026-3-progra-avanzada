using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Quiniela.Api.Hubs;
using Quiniela.BusinessLogic;
using Quiniela.Model;

namespace Quiniela.Api.Controllers;

[Route("api/forecasts")]
public sealed class ForecastsController : CrudController<Forecast>
{
    private readonly ForecastCrudService forecastService;
    private readonly IHubContext<ForecastsHub> forecastsHub;

    public ForecastsController(
        ForecastCrudService crudService,
        IHubContext<ForecastsHub> forecastsHub) : base(crudService)
    {
        forecastService = crudService;
        this.forecastsHub = forecastsHub;
    }

    [HttpGet("by-matches")]
    public Task<List<Forecast>> GetByMatches(
        [FromQuery] int[] matchIds,
        CancellationToken cancellationToken) =>
        forecastService.GetForMatchesAsync(matchIds, cancellationToken);

    [HttpGet("{forecastId:int}")]
    public Task<ActionResult<Forecast>> GetById(int forecastId, CancellationToken cancellationToken) =>
        GetByKeyAsync(new object[] { forecastId }, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<Forecast>> Create(Forecast forecast, CancellationToken cancellationToken)
    {
        Forecast createdForecast = await forecastService.InsertAsync(forecast, cancellationToken);
        await NotifyForecastChangedAsync(createdForecast.MatchId);
        return CreatedAtAction(
            nameof(GetById),
            new { forecastId = createdForecast.ForecastId },
            createdForecast);
    }

    [HttpPut("{forecastId:int}")]
    public async Task<IActionResult> Update(
        int forecastId,
        Forecast forecast,
        CancellationToken cancellationToken)
    {
        if (forecastId != forecast.ForecastId)
        {
            return BadRequest("The route key must match the entity key.");
        }

        int? previousMatchId = await forecastService.GetMatchIdAsync(forecastId, cancellationToken);
        if (!previousMatchId.HasValue)
        {
            return NotFound();
        }

        await forecastService.UpdateAsync(forecast, cancellationToken);
        await NotifyForecastChangedAsync(previousMatchId.Value);
        if (forecast.MatchId != previousMatchId.Value)
        {
            await NotifyForecastChangedAsync(forecast.MatchId);
        }

        return NoContent();
    }

    [HttpDelete("{forecastId:int}")]
    public async Task<IActionResult> Delete(int forecastId, CancellationToken cancellationToken)
    {
        int? matchId = await forecastService.GetMatchIdAsync(forecastId, cancellationToken);
        if (!matchId.HasValue)
        {
            return NotFound();
        }

        if (!await forecastService.DeleteAsync(new object[] { forecastId }, cancellationToken))
        {
            return NotFound();
        }

        await NotifyForecastChangedAsync(matchId.Value);
        return NoContent();
    }

    private Task NotifyForecastChangedAsync(int matchId) =>
        forecastsHub.Clients
            .Group(ForecastsHub.GetMatchGroup(matchId))
            .SendAsync("ForecastsChanged", matchId);
}