using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Quiniela.BusinessLogic;
using Quiniela.Model;
using Quiniela.Mvc.Models;

namespace Quiniela.Mvc.Controllers;

public sealed class ForecastsChartController(
    QuinielaDbContext context,
    IHttpClientFactory httpClientFactory) : Controller
{
    private readonly HttpClient apiClient = httpClientFactory.CreateClient("QuinielaApi");

    [HttpGet]
    public IActionResult Index()
    {
        Uri apiBaseAddress = apiClient.BaseAddress
            ?? throw new InvalidOperationException("The Quiniela API base address is not configured.");

        return View(new ForecastsChartViewModel
        {
            SportOptions = Enum.GetValues<SportType>()
                .Select(sport => new SelectListItem(
                    GetSportLabel(sport),
                    sport.ToString()))
                .ToList(),
            HubUrl = new Uri(apiBaseAddress, "hubs/forecasts").AbsoluteUri
        });
    }

    [HttpGet]
    public async Task<IActionResult> Teams(SportType sport, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(sport))
        {
            return BadRequest("Choose a valid sport.");
        }

        List<ForecastChartTeamOption> teams = await context.Teams.AsNoTracking()
            .Where(team => team.Sport == sport)
            .OrderBy(team => team.Name)
            .Select(team => new ForecastChartTeamOption(team.TeamId, team.Name))
            .ToListAsync(cancellationToken);

        return Json(teams);
    }

    [HttpGet]
    public async Task<IActionResult> Matches(int teamId, CancellationToken cancellationToken)
    {
        if (teamId < 1)
        {
            return BadRequest("A valid team id is required.");
        }

        var matches = await (
            from match in context.Matches.AsNoTracking()
            where match.HomeTeamId == teamId || match.AwayTeamId == teamId
            join homeTeam in context.Teams.AsNoTracking() on match.HomeTeamId equals homeTeam.TeamId
            join awayTeam in context.Teams.AsNoTracking() on match.AwayTeamId equals awayTeam.TeamId
            orderby match.KickoffAt, match.MatchId
            select new
            {
                match.MatchId,
                HomeTeamName = homeTeam.Name,
                AwayTeamName = awayTeam.Name,
                match.KickoffAt,
                match.TimeZone,
                match.Status
            })
            .ToListAsync(cancellationToken);

        return Json(matches.Select(match => new ForecastChartMatchOption(
            match.MatchId,
            $"{match.HomeTeamName} vs {match.AwayTeamName} · {match.KickoffAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}",
            match.KickoffAt,
            match.TimeZone,
            match.Status.ToString())));
    }

    [HttpGet]
    public async Task<IActionResult> ChartData(int matchId, CancellationToken cancellationToken)
    {
        if (matchId < 1)
        {
            return BadRequest("A valid match id is required.");
        }

        using HttpResponseMessage response = await apiClient.GetAsync(
            $"api/forecast-queries/by-match?matchId={matchId.ToString(CultureInfo.InvariantCulture)}",
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return await ForwardApiErrorAsync(response, cancellationToken);
        }

        List<ForecastCountByHour> counts =
            await ReadRequiredJsonAsync<List<ForecastCountByHour>>(response, cancellationToken);
        return Json(counts);
    }

    private static string GetSportLabel(SportType sport) =>
        sport == SportType.AmericanFootball ? "American football" : sport.ToString();

    private static async Task<T> ReadRequiredJsonAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        T? value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        return value ?? throw new InvalidOperationException(
            $"The API returned an empty {typeof(T).Name} response.");
    }

    private async Task<IActionResult> ForwardApiErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        string message = await response.Content.ReadAsStringAsync(cancellationToken);
        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest => BadRequest(message),
            HttpStatusCode.NotFound => NotFound(),
            HttpStatusCode.Conflict => Conflict(message),
            _ => StatusCode((int)response.StatusCode, message)
        };
    }
}
