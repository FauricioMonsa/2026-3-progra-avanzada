using Microsoft.AspNetCore.Mvc;
using Quiniela.BusinessLogic;
using Quiniela.Model;

namespace Quiniela.Api.Controllers;

[Route("api/forecasts")]
public sealed class ForecastsController : CrudController<Forecast>
{
    private readonly ForecastCrudService forecastService;

    public ForecastsController(ForecastCrudService crudService) : base(crudService)
    {
        forecastService = crudService;
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
    public Task<ActionResult<Forecast>> Create(Forecast forecast, CancellationToken cancellationToken) =>
        CreateAsync(forecast, nameof(GetById), new { forecastId = forecast.ForecastId }, cancellationToken);

    [HttpPut("{forecastId:int}")]
    public Task<IActionResult> Update(
        int forecastId,
        Forecast forecast,
        CancellationToken cancellationToken) =>
        UpdateAsync(
            new object[] { forecastId },
            new object[] { forecast.ForecastId },
            forecast,
            cancellationToken);

    [HttpDelete("{forecastId:int}")]
    public Task<IActionResult> Delete(int forecastId, CancellationToken cancellationToken) =>
        DeleteAsync(new object[] { forecastId }, cancellationToken);
}