using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Quiniela.BusinessLogic;
using Quiniela.Model;
using Quiniela.Mvc.Models;

namespace Quiniela.Mvc.Controllers;

public sealed class MatchesJsController(IHttpClientFactory httpClientFactory) : Controller
{
    private readonly HttpClient apiClient = httpClientFactory.CreateClient("QuinielaApi");

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        Task<IReadOnlyList<Tournament>> tournamentsTask =
            GetAllApiItemsAsync<Tournament>("api/tournaments", cancellationToken);
        Task<IReadOnlyList<Team>> teamsTask =
            GetAllApiItemsAsync<Team>("api/teams", cancellationToken);
        await Task.WhenAll(tournamentsTask, teamsTask);

        return View(new MatchesJsIndexViewModel
        {
            TournamentOptions = (await tournamentsTask)
                .OrderBy(tournament => tournament.Name)
                .ThenBy(tournament => tournament.SeasonYear)
                .Select(tournament => new SelectListItem(
                    $"{tournament.Name} ({tournament.SeasonYear}, {tournament.Sport})",
                    tournament.TournamentId.ToString(CultureInfo.InvariantCulture)))
                .ToList(),
            TeamOptions = (await teamsTask)
                .OrderBy(team => team.Name)
                .Select(team => new SelectListItem(
                    $"{team.Name} ({team.Sport})",
                    team.TeamId.ToString(CultureInfo.InvariantCulture)))
                .ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Matches(
        int? tournamentId,
        int? teamId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize is not (5 or 10 or 20 or 50))
        {
            return BadRequest("Choose a valid page and page size.");
        }

        if (tournamentId.HasValue && teamId.HasValue)
        {
            return BadRequest("Choose either a tournament or a team filter, not both.");
        }

        List<string> query =
        [
            $"pageNumber={pageNumber.ToString(CultureInfo.InvariantCulture)}",
            $"pageSize={pageSize.ToString(CultureInfo.InvariantCulture)}"
        ];
        if (tournamentId.HasValue)
        {
            query.Add($"tournamentId={tournamentId.Value.ToString(CultureInfo.InvariantCulture)}");
        }
        else if (teamId.HasValue)
        {
            query.Add($"teamId={teamId.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        using HttpResponseMessage response = await apiClient.GetAsync(
            $"api/matches/details?{string.Join("&", query)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return await ForwardApiErrorAsync(response, cancellationToken);
        }

        List<MatchDetails> matches = await ReadRequiredJsonAsync<List<MatchDetails>>(
            response, cancellationToken);
        if (!response.Headers.TryGetValues("X-Total-Count", out IEnumerable<string>? counts)
            || !int.TryParse(counts.SingleOrDefault(), CultureInfo.InvariantCulture, out int totalCount))
        {
            throw new InvalidOperationException(
                "The matches API response did not include a valid X-Total-Count header.");
        }

        return Json(new { matches, totalCount, pageNumber, pageSize });
    }

    [HttpGet]
    public async Task<IActionResult> Forecasts(int matchId, CancellationToken cancellationToken)
    {
        if (matchId < 1)
        {
            return BadRequest("A valid match id is required.");
        }

        using HttpResponseMessage response = await apiClient.GetAsync(
            $"api/forecasts/by-matches?matchIds={matchId.ToString(CultureInfo.InvariantCulture)}",
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return await ForwardApiErrorAsync(response, cancellationToken);
        }

        List<Forecast> forecasts = await ReadRequiredJsonAsync<List<Forecast>>(
            response, cancellationToken);
        return Json(forecasts.Select(forecast => new
        {
            forecast.ForecastId,
            forecast.UserId,
            forecast.HomeScore,
            forecast.AwayScore,
            forecast.SubmittedAt
        }));
    }

    private async Task<IReadOnlyList<T>> GetAllApiItemsAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        const int pageSize = 100;
        List<T> items = [];
        for (int pageNumber = 1; ; pageNumber++)
        {
            using HttpResponseMessage response = await apiClient.GetAsync(
                $"{path}?pageNumber={pageNumber}&pageSize={pageSize}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"API request failed with {(int)response.StatusCode} ({response.StatusCode}): {body}",
                    null,
                    response.StatusCode);
            }

            List<T> page = await ReadRequiredJsonAsync<List<T>>(response, cancellationToken);
            items.AddRange(page);
            if (page.Count < pageSize)
            {
                return items;
            }
        }
    }

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
