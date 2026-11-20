using Microsoft.AspNetCore.Mvc;
using Quiniela.BusinessLogic;

namespace Quiniela.Api.Controllers;

[ApiController]
[Route("api/forecast-queries")]
public sealed class ForecastQueriesController(IForecastQueryService forecastQueryService) : ControllerBase
{
    [HttpGet("by-match")]
    public async Task<ActionResult<List<ForecastCountByHour>>> GetByMatch(
        [FromQuery] int matchId,
        CancellationToken cancellationToken)
    {
        if (matchId < 1)
        {
            return BadRequest("A valid match id is required.");
        }

        return Ok(await forecastQueryService.GetCountsByHourAsync(matchId, cancellationToken));
    }
}
