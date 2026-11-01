using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Quiniela.BusinessLogic;
using Quiniela.Model;
using Quiniela.Mvc.Models;

namespace Quiniela.Mvc.Controllers;

public sealed class MatchesApiController(
    IHttpClientFactory httpClientFactory,
    ILogger<MatchesApiController> logger) : Controller
{
    private const string MatchesViewPath = "~/Views/Matches/";
    private static readonly int[] PageSizes = [5, 10, 20, 50];
    private readonly HttpClient apiClient = httpClientFactory.CreateClient("QuinielaApi");

    [HttpGet]
    public async Task<IActionResult> Index(
        int? tournamentId,
        int? teamId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            return BadRequest("Page number must be greater than zero.");
        }

        if (tournamentId.HasValue && teamId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Choose either a tournament or a team filter, not both.");
        }

        pageSize = PageSizes.Contains(pageSize) ? pageSize : 10;
        if (pageNumber > int.MaxValue / pageSize)
        {
            return BadRequest("The requested page is out of range.");
        }

        string query = $"pageNumber={pageNumber}&pageSize={pageSize}";
        if (tournamentId.HasValue)
        {
            query += $"&tournamentId={tournamentId.Value}";
        }
        else if (teamId.HasValue)
        {
            query += $"&teamId={teamId.Value}";
        }

        Task<HttpResponseMessage> matchesResponseTask =
            apiClient.GetAsync($"api/matches/details?{query}", cancellationToken);
        Task<IReadOnlyList<Tournament>> tournamentsTask =
            GetAllApiItemsAsync<Tournament>("api/tournaments", cancellationToken);
        Task<IReadOnlyList<Team>> teamsTask =
            GetAllApiItemsAsync<Team>("api/teams", cancellationToken);
        await Task.WhenAll(matchesResponseTask, tournamentsTask, teamsTask);

        using HttpResponseMessage matchesResponse = await matchesResponseTask;
        if (!matchesResponse.IsSuccessStatusCode)
        {
            return await ApiErrorResultAsync(matchesResponse, cancellationToken);
        }

        List<MatchDetails> results = await ReadRequiredJsonAsync<List<MatchDetails>>(
            matchesResponse, cancellationToken);
        if (!matchesResponse.Headers.TryGetValues("X-Total-Count", out IEnumerable<string>? counts)
            || !int.TryParse(counts.SingleOrDefault(), CultureInfo.InvariantCulture, out int totalCount))
        {
            throw new InvalidOperationException("The matches API response did not include a valid X-Total-Count header.");
        }

        List<MatchMasterViewModel> matches = CreateMasterRows(results);
        await LoadForecastsAsync(matches, cancellationToken);

        return View($"{MatchesViewPath}Index.cshtml", new MatchesIndexViewModel
        {
            Matches = matches,
            TournamentOptions = CreateTournamentOptions(await tournamentsTask, tournamentId),
            TeamOptions = CreateTeamOptions(await teamsTask, teamId),
            TournamentId = tournamentId,
            TeamId = teamId,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    [HttpGet]
    public async Task<IActionResult> Stats(int matchId, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await apiClient.GetAsync(
            $"api/matches/stats/{matchId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        if (!response.IsSuccessStatusCode)
        {
            return await ApiErrorResultAsync(response, cancellationToken);
        }

        string stats = await response.Content.ReadAsStringAsync(cancellationToken);
        return Content(stats, "application/json; charset=utf-8");
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        MatchMasterViewModel? match = await GetMatchMasterAsync(id, cancellationToken);
        return match is null ? NotFound() : View($"{MatchesViewPath}Details.cshtml", match);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        MatchFormViewModel model = new()
        {
            KickoffAt = DateTime.UtcNow.AddMinutes(90)
        };
        await PopulateMatchFormOptionsAsync(model, cancellationToken);
        return View($"{MatchesViewPath}Create.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        MatchFormViewModel model,
        CancellationToken cancellationToken)
    {
        await ValidateMatchFormAsync(model, cancellationToken);
        if (!ModelState.IsValid)
        {
            await PopulateMatchFormOptionsAsync(model, cancellationToken);
            return View($"{MatchesViewPath}Create.cshtml", model);
        }

        DateTime kickoffAt = DateTime.SpecifyKind(model.KickoffAt, DateTimeKind.Utc);
        Match match = new()
        {
            Kind = model.Kind,
            TournamentId = model.Kind == MatchKind.Competition ? model.TournamentId : null,
            RoundName = model.Kind == MatchKind.Competition ? model.RoundName : null,
            Sport = model.Sport,
            HomeTeamId = model.HomeTeamId,
            AwayTeamId = model.AwayTeamId,
            KickoffAt = kickoffAt,
            TimeZone = "UTC",
            MatchDay = DateOnly.FromDateTime(kickoffAt),
            Status = MatchStatus.Scheduled,
            Stats = "{}"
        };

        using HttpResponseMessage response = await apiClient.PostAsJsonAsync(
            "api/matches", match, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Matches API rejected match creation with status {StatusCode}.", response.StatusCode);
            return await ApiErrorResultAsync(response, cancellationToken);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        Match? match = await GetOptionalApiItemAsync<Match>($"api/matches/{id}", cancellationToken);
        if (match is null)
        {
            return NotFound();
        }

        if (match.Status == MatchStatus.Finished)
        {
            return Conflict("Finished matches cannot be edited.");
        }

        MatchEditViewModel model = new()
        {
            MatchId = match.MatchId,
            CurrentStatus = match.Status,
            NewStatus = match.Status == MatchStatus.Scheduled
                ? MatchStatus.Live
                : MatchStatus.Finished,
            HomeScore = match.HomeScore,
            AwayScore = match.AwayScore,
            Stats = match.Stats,
            StatusOptions = GetStatusOptions(match.Status)
        };
        return View($"{MatchesViewPath}Edit.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        MatchEditViewModel model,
        CancellationToken cancellationToken)
    {
        if (id != model.MatchId)
        {
            return BadRequest();
        }

        Match? match = await GetOptionalApiItemAsync<Match>($"api/matches/{id}", cancellationToken);
        if (match is null)
        {
            return NotFound();
        }

        MatchStatus nextStatus = match.Status == MatchStatus.Scheduled
            ? MatchStatus.Live
            : MatchStatus.Finished;
        if (match.Status == MatchStatus.Finished || model.NewStatus != nextStatus)
        {
            ModelState.AddModelError(nameof(model.NewStatus), "Choose the next valid match status.");
        }

        if (match.Status == MatchStatus.Live
            && (!model.HomeScore.HasValue || !model.AwayScore.HasValue))
        {
            ModelState.AddModelError(string.Empty, "Both final scores are required to finish a match.");
        }

        if (match.Status == MatchStatus.Live
            && (string.IsNullOrWhiteSpace(model.Stats) || !IsValidJson(model.Stats)))
        {
            ModelState.AddModelError(nameof(model.Stats), "Stats must contain valid JSON.");
        }

        if (!ModelState.IsValid)
        {
            model.CurrentStatus = match.Status;
            model.StatusOptions = GetStatusOptions(match.Status);
            return View($"{MatchesViewPath}Edit.cshtml", model);
        }

        Match update = new()
        {
            MatchId = match.MatchId,
            Kind = match.Kind,
            TournamentId = match.TournamentId,
            RoundName = match.RoundName,
            HomeTeamId = match.HomeTeamId,
            AwayTeamId = match.AwayTeamId,
            Sport = match.Sport,
            KickoffAt = match.KickoffAt,
            TimeZone = match.TimeZone,
            MatchDay = match.MatchDay,
            HomeScore = match.HomeScore,
            AwayScore = match.AwayScore,
            Status = model.NewStatus,
            Stats = match.Stats
        };

        if (update.Status == MatchStatus.Finished)
        {
            update.HomeScore = model.HomeScore;
            update.AwayScore = model.AwayScore;
            update.Stats = model.Stats;
        }

        using HttpResponseMessage response = await apiClient.PutAsJsonAsync(
            $"api/matches/{id}", update, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Matches API rejected the update for match {MatchId} with status {StatusCode}.",
                id, response.StatusCode);
            return await ApiErrorResultAsync(response, cancellationToken);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        MatchMasterViewModel? match = await GetMatchMasterAsync(id, cancellationToken);
        return match is null ? NotFound() : View($"{MatchesViewPath}Delete.cshtml", match);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await apiClient.DeleteAsync(
            $"api/matches/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Matches API rejected deletion for match {MatchId} with status {StatusCode}.",
                id, response.StatusCode);
            return await ApiErrorResultAsync(response, cancellationToken);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<MatchMasterViewModel?> GetMatchMasterAsync(
        int matchId,
        CancellationToken cancellationToken)
    {
        MatchDetails? match = await GetOptionalApiItemAsync<MatchDetails>(
            $"api/matches/details/{matchId}", cancellationToken);
        if (match is null)
        {
            return null;
        }

        MatchMasterViewModel row = CreateMasterRows([match])[0];
        List<MatchMasterViewModel> rows = [row];
        await LoadForecastsAsync(rows, cancellationToken);
        return rows[0];
    }

    private async Task LoadForecastsAsync(
        List<MatchMasterViewModel> matches,
        CancellationToken cancellationToken)
    {
        if (matches.Count == 0)
        {
            return;
        }

        string matchIds = string.Join("&", matches.Select(match => $"matchIds={match.MatchId}"));
        List<Forecast> forecasts = await GetRequiredApiItemAsync<List<Forecast>>(
            $"api/forecasts/by-matches?{matchIds}", cancellationToken);
        Dictionary<int, IReadOnlyList<ForecastDetailViewModel>> forecastsByMatch =
            forecasts.GroupBy(forecast => forecast.MatchId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<ForecastDetailViewModel>)group.Select(forecast =>
                        new ForecastDetailViewModel
                        {
                            UserId = forecast.UserId,
                            PredictedHomeScore = forecast.HomeScore,
                            PredictedAwayScore = forecast.AwayScore,
                            SubmittedAt = forecast.SubmittedAt
                        }).ToList());

        for (int index = 0; index < matches.Count; index++)
        {
            MatchMasterViewModel match = matches[index];
            matches[index] = new MatchMasterViewModel
            {
                MatchId = match.MatchId,
                TournamentName = match.TournamentName,
                Kind = match.Kind,
                SportName = match.SportName,
                HomeTeamName = match.HomeTeamName,
                AwayTeamName = match.AwayTeamName,
                PlayedAt = match.PlayedAt,
                Status = match.Status,
                HomeScore = match.HomeScore,
                AwayScore = match.AwayScore,
                Forecasts = forecastsByMatch.GetValueOrDefault(match.MatchId, [])
            };
        }
    }

    private async Task ValidateMatchFormAsync(
        MatchFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.HomeTeamId == model.AwayTeamId)
        {
            ModelState.AddModelError(nameof(model.AwayTeamId), "Home and away teams must be different.");
        }

        if (model.Kind == MatchKind.Competition && !model.TournamentId.HasValue)
        {
            ModelState.AddModelError(nameof(model.TournamentId), "Select a tournament for a competition match.");
        }

        if (model.Kind == MatchKind.Friendly && model.TournamentId.HasValue)
        {
            ModelState.AddModelError(nameof(model.TournamentId), "Friendly matches cannot belong to a tournament.");
        }

        Team? homeTeam = await GetOptionalApiItemAsync<Team>(
            $"api/teams/{model.HomeTeamId}", cancellationToken);
        Team? awayTeam = await GetOptionalApiItemAsync<Team>(
            $"api/teams/{model.AwayTeamId}", cancellationToken);
        if (homeTeam is null || homeTeam.Sport != model.Sport)
        {
            ModelState.AddModelError(nameof(model.HomeTeamId), "Select a home team in the chosen sport.");
        }

        if (awayTeam is null || awayTeam.Sport != model.Sport)
        {
            ModelState.AddModelError(nameof(model.AwayTeamId), "Select an away team in the chosen sport.");
        }

        if (model.Kind == MatchKind.Competition && model.TournamentId.HasValue)
        {
            Tournament? tournament = await GetOptionalApiItemAsync<Tournament>(
                $"api/tournaments/{model.TournamentId.Value}", cancellationToken);
            if (tournament is null || tournament.Sport != model.Sport)
            {
                ModelState.AddModelError(nameof(model.TournamentId), "Select a tournament in the chosen sport.");
            }
            else if (homeTeam is not null && awayTeam is not null)
            {
                TournamentTeam? homeRegistration = await GetOptionalApiItemAsync<TournamentTeam>(
                    $"api/tournament-participants/{tournament.TournamentId}/{homeTeam.TeamId}",
                    cancellationToken);
                TournamentTeam? awayRegistration = await GetOptionalApiItemAsync<TournamentTeam>(
                    $"api/tournament-participants/{tournament.TournamentId}/{awayTeam.TeamId}",
                    cancellationToken);
                if (homeRegistration is null || awayRegistration is null)
                {
                    ModelState.AddModelError(
                        nameof(model.TournamentId),
                        "Both teams must be registered in the selected tournament.");
                }
            }
        }
    }

    private async Task PopulateMatchFormOptionsAsync(
        MatchFormViewModel model,
        CancellationToken cancellationToken)
    {
        Task<IReadOnlyList<Tournament>> tournamentsTask =
            GetAllApiItemsAsync<Tournament>("api/tournaments", cancellationToken);
        Task<IReadOnlyList<Team>> teamsTask =
            GetAllApiItemsAsync<Team>("api/teams", cancellationToken);
        await Task.WhenAll(tournamentsTask, teamsTask);
        model.TournamentOptions = CreateTournamentOptions(await tournamentsTask, model.TournamentId);
        model.TeamOptions = CreateTeamOptions(await teamsTask, null);
    }

    private async Task<IReadOnlyList<T>> GetAllApiItemsAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        const int pageSize = 100;
        List<T> items = [];
        for (int pageNumber = 1; ; pageNumber++)
        {
            List<T> page = await GetRequiredApiItemAsync<List<T>>(
                $"{path}?pageNumber={pageNumber}&pageSize={pageSize}", cancellationToken);
            items.AddRange(page);
            if (page.Count < pageSize)
            {
                return items;
            }
        }
    }

    private async Task<T> GetRequiredApiItemAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await apiClient.GetAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await ThrowApiErrorAsync(response, cancellationToken);
        }

        return await ReadRequiredJsonAsync<T>(response, cancellationToken);
    }

    private async Task<T?> GetOptionalApiItemAsync<T>(
        string path,
        CancellationToken cancellationToken) where T : class
    {
        using HttpResponseMessage response = await apiClient.GetAsync(path, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            await ThrowApiErrorAsync(response, cancellationToken);
        }

        return await ReadRequiredJsonAsync<T>(response, cancellationToken);
    }

    private static async Task<T> ReadRequiredJsonAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        T? value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        return value ?? throw new InvalidOperationException(
            $"The API returned an empty {typeof(T).Name} response.");
    }

    private static async Task ThrowApiErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"API request failed with {(int)response.StatusCode} ({response.StatusCode}): {body}",
            null,
            response.StatusCode);
    }

    private async Task<IActionResult> ApiErrorResultAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        string message = await response.Content.ReadAsStringAsync(cancellationToken);
        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest => BadRequest(message),
            HttpStatusCode.Conflict => Conflict(message),
            HttpStatusCode.NotFound => NotFound(),
            _ => StatusCode((int)response.StatusCode, message)
        };
    }

    private static List<MatchMasterViewModel> CreateMasterRows(IReadOnlyList<MatchDetails> results) =>
        results.Select(result => new MatchMasterViewModel
        {
            MatchId = result.MatchId,
            TournamentName = result.TournamentName,
            Kind = result.MatchKind,
            SportName = result.SportName,
            HomeTeamName = result.HomeTeamName,
            AwayTeamName = result.AwayTeamName,
            PlayedAt = result.DatePlayed,
            Status = Enum.Parse<MatchStatus>(result.StatusName),
            HomeScore = result.HomeScore,
            AwayScore = result.AwayScore
        }).ToList();

    private static IReadOnlyList<SelectListItem> CreateTournamentOptions(
        IEnumerable<Tournament> tournaments,
        int? selectedId) =>
        tournaments.OrderBy(tournament => tournament.Name)
            .ThenBy(tournament => tournament.SeasonYear)
            .Select(tournament => new SelectListItem
            {
                Value = tournament.TournamentId.ToString(CultureInfo.InvariantCulture),
                Text = $"{tournament.Name} ({tournament.SeasonYear}, {tournament.Sport})",
                Selected = selectedId == tournament.TournamentId
            }).ToList();

    private static IReadOnlyList<SelectListItem> CreateTeamOptions(
        IEnumerable<Team> teams,
        int? selectedId) =>
        teams.OrderBy(team => team.Name)
            .Select(team => new SelectListItem
            {
                Value = team.TeamId.ToString(CultureInfo.InvariantCulture),
                Text = $"{team.Name} ({team.Sport})",
                Selected = selectedId == team.TeamId
            }).ToList();

    private static IReadOnlyList<SelectListItem> GetStatusOptions(MatchStatus currentStatus)
    {
        MatchStatus nextStatus = currentStatus == MatchStatus.Scheduled
            ? MatchStatus.Live
            : MatchStatus.Finished;
        return [new SelectListItem(nextStatus.ToString(), nextStatus.ToString(), true)];
    }

    private static bool IsValidJson(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
