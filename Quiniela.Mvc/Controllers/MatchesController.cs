using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Quiniela.BusinessLogic;
using Quiniela.Model;
using Quiniela.Mvc.Models;

namespace Quiniela.Mvc.Controllers;

public sealed class MatchesController(
    MatchCrudService matchService,
    QuinielaDbContext context,
    ILogger<MatchesController> logger) : Controller
{
    private static readonly int[] PageSizes = [5, 10, 20, 50];

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

        IQueryable<Match> matchesQuery = context.Matches.AsNoTracking();
        if (tournamentId.HasValue)
        {
            matchesQuery = matchesQuery.Where(match => match.TournamentId == tournamentId.Value);
        }
        else if (teamId.HasValue)
        {
            matchesQuery = matchesQuery.Where(match =>
                match.HomeTeamId == teamId.Value || match.AwayTeamId == teamId.Value);
        }

        int totalCount = await matchesQuery.CountAsync(cancellationToken);
        List<MatchMasterViewModel> matches;

        if (tournamentId.HasValue)
        {
            List<MatchDetails> results = await matchService.GetMatchesByTournamentAsync(
                tournamentId.Value, pageNumber, pageSize, cancellationToken);
            matches = CreateMasterRows(results);
        }
        else if (teamId.HasValue)
        {
            List<MatchDetails> results = await matchService.GetMatchesByTeamAsync(
                teamId.Value,
                DateOnly.MinValue,
                DateOnly.MaxValue,
                pageNumber,
                pageSize,
                cancellationToken);
            matches = CreateMasterRows(results);
        }
        else
        {
            matches = await GetAllMatchRowsAsync(pageNumber, pageSize, cancellationToken);
        }

        await LoadForecastsAsync(matches, cancellationToken);

        return View(new MatchesIndexViewModel
        {
            Matches = matches,
            TournamentOptions = await GetTournamentOptionsAsync(tournamentId, cancellationToken),
            TeamOptions = await GetTeamOptionsAsync(teamId, cancellationToken),
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
        Match? match = await context.Matches.AsNoTracking()
            .SingleOrDefaultAsync(item => item.MatchId == matchId, cancellationToken);
        return match is null
            ? NotFound()
            : Content(match.Stats, "application/json; charset=utf-8");
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        MatchMasterViewModel? match = await GetMatchMasterAsync(id, cancellationToken);
        return match is null ? NotFound() : View(match);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        MatchFormViewModel model = new()
        {
            KickoffAt = DateTime.UtcNow.AddMinutes(90)
        };
        await PopulateMatchFormOptionsAsync(model, cancellationToken);
        return View(model);
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
            return View(model);
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

        try
        {
            await matchService.InsertAsync(match, cancellationToken);
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Database rejected the new match.");
            ModelState.AddModelError(string.Empty, "The match could not be saved. Check the teams, tournament, and kickoff time.");
            await PopulateMatchFormOptionsAsync(model, cancellationToken);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        Match? match = await context.Matches.AsNoTracking()
            .SingleOrDefaultAsync(item => item.MatchId == id, cancellationToken);
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
            StatusOptions = GetStatusOptions(match.Status)
        };
        return View(model);
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

        Match? match = await context.Matches.SingleOrDefaultAsync(
            item => item.MatchId == id, cancellationToken);
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
            return View(model);
        }

        match.Status = model.NewStatus;
        if (match.Status == MatchStatus.Finished)
        {
            match.HomeScore = model.HomeScore;
            match.AwayScore = model.AwayScore;
            match.Stats = model.Stats;
        }

        try
        {
            await matchService.UpdateAsync(match, cancellationToken);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Database rejected the match update for match {MatchId}.", id);
            ModelState.AddModelError(string.Empty, "The match could not be updated. Check its current status and final scores.");
            model.CurrentStatus = match.Status == MatchStatus.Live
                ? MatchStatus.Scheduled
                : MatchStatus.Live;
            model.StatusOptions = GetStatusOptions(model.CurrentStatus);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        MatchMasterViewModel? match = await GetMatchMasterAsync(id, cancellationToken);
        return match is null ? NotFound() : View(match);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        Match? match = await context.Matches.SingleOrDefaultAsync(
            item => item.MatchId == id, cancellationToken);
        if (match is null)
        {
            return NotFound();
        }

        List<Forecast> forecasts = await context.Forecasts
            .Where(forecast => forecast.MatchId == id)
            .ToListAsync(cancellationToken);
        context.Forecasts.RemoveRange(forecasts);
        context.Matches.Remove(match);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Database rejected deletion of match {MatchId}.", id);
            return Conflict("The match could not be deleted because related data still references it.");
        }
    }

    private static List<MatchMasterViewModel> CreateMasterRows(IReadOnlyList<MatchDetails> results)
    {
        if (results.Count == 0)
        {
            return [];
        }

        return results.Select(result => new MatchMasterViewModel
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
    }

    private async Task<List<MatchMasterViewModel>> GetAllMatchRowsAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var matches = await (
            from match in context.Matches.AsNoTracking()
            join homeTeam in context.Teams.AsNoTracking() on match.HomeTeamId equals homeTeam.TeamId
            join awayTeam in context.Teams.AsNoTracking() on match.AwayTeamId equals awayTeam.TeamId
            join tournament in context.Tournaments.AsNoTracking()
                on match.TournamentId equals (int?)tournament.TournamentId into tournamentGroup
            from tournament in tournamentGroup.DefaultIfEmpty()
            orderby match.KickoffAt, match.MatchId
            select new
            {
                match.MatchId,
                TournamentName = tournament == null ? null : tournament.Name,
                match.Kind,
                match.Sport,
                HomeTeamName = homeTeam.Name,
                AwayTeamName = awayTeam.Name,
                match.KickoffAt,
                match.Status,
                match.HomeScore,
                match.AwayScore
            })
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return matches.Select(match => new MatchMasterViewModel
        {
            MatchId = match.MatchId,
            TournamentName = match.TournamentName,
            Kind = match.Kind,
            SportName = match.Sport.ToString(),
            HomeTeamName = match.HomeTeamName,
            AwayTeamName = match.AwayTeamName,
            PlayedAt = match.KickoffAt,
            Status = match.Status,
            HomeScore = match.HomeScore,
            AwayScore = match.AwayScore
        }).ToList();
    }

    private async Task LoadForecastsAsync(
        List<MatchMasterViewModel> matches,
        CancellationToken cancellationToken)
    {
        if (matches.Count == 0)
        {
            return;
        }

        int[] matchIds = matches.Select(match => match.MatchId).ToArray();
        var forecasts = await context.Forecasts.AsNoTracking()
            .Where(forecast => matchIds.Contains(forecast.MatchId))
            .OrderBy(forecast => forecast.UserId)
            .ThenBy(forecast => forecast.SubmittedAt)
            .Select(forecast => new
            {
                forecast.MatchId,
                forecast.UserId,
                forecast.HomeScore,
                forecast.AwayScore,
                forecast.SubmittedAt
            })
            .ToListAsync(cancellationToken);

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

    private async Task<MatchMasterViewModel?> GetMatchMasterAsync(
        int matchId,
        CancellationToken cancellationToken)
    {
        var match = await (
            from item in context.Matches.AsNoTracking()
            where item.MatchId == matchId
            join homeTeam in context.Teams.AsNoTracking() on item.HomeTeamId equals homeTeam.TeamId
            join awayTeam in context.Teams.AsNoTracking() on item.AwayTeamId equals awayTeam.TeamId
            join tournament in context.Tournaments.AsNoTracking()
                on item.TournamentId equals (int?)tournament.TournamentId into tournamentGroup
            from tournament in tournamentGroup.DefaultIfEmpty()
            select new MatchMasterViewModel
            {
                MatchId = item.MatchId,
                TournamentName = tournament == null ? null : tournament.Name,
                Kind = item.Kind,
                SportName = item.Sport.ToString(),
                HomeTeamName = homeTeam.Name,
                AwayTeamName = awayTeam.Name,
                PlayedAt = item.KickoffAt,
                Status = item.Status,
                HomeScore = item.HomeScore,
                AwayScore = item.AwayScore,
            }).SingleOrDefaultAsync(cancellationToken);

        if (match is null)
        {
            return null;
        }

        List<MatchMasterViewModel> matches = [match];
        await LoadForecastsAsync(matches, cancellationToken);
        return matches[0];
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

        Team? homeTeam = await context.Teams.AsNoTracking()
            .SingleOrDefaultAsync(team => team.TeamId == model.HomeTeamId, cancellationToken);
        Team? awayTeam = await context.Teams.AsNoTracking()
            .SingleOrDefaultAsync(team => team.TeamId == model.AwayTeamId, cancellationToken);
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
            Tournament? tournament = await context.Tournaments.AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.TournamentId == model.TournamentId.Value,
                    cancellationToken);
            if (tournament is null || tournament.Sport != model.Sport)
            {
                ModelState.AddModelError(nameof(model.TournamentId), "Select a tournament in the chosen sport.");
            }
            else if (homeTeam is not null && awayTeam is not null)
            {
                bool teamsRegistered = await context.TournamentTeams.AsNoTracking()
                    .AnyAsync(
                        item => item.TournamentId == tournament.TournamentId
                            && item.TeamId == homeTeam.TeamId,
                        cancellationToken)
                    && await context.TournamentTeams.AsNoTracking()
                        .AnyAsync(
                            item => item.TournamentId == tournament.TournamentId
                                && item.TeamId == awayTeam.TeamId,
                            cancellationToken);
                if (!teamsRegistered)
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
        model.TournamentOptions = await GetTournamentOptionsAsync(
            model.TournamentId, cancellationToken);
        model.TeamOptions = await GetTeamOptionsAsync(null, cancellationToken);
    }

    private async Task<IReadOnlyList<SelectListItem>> GetTournamentOptionsAsync(
        int? selectedId,
        CancellationToken cancellationToken) =>
        await context.Tournaments.AsNoTracking()
            .OrderBy(tournament => tournament.Name)
            .ThenBy(tournament => tournament.SeasonYear)
            .Select(tournament => new SelectListItem
            {
                Value = tournament.TournamentId.ToString(),
                Text = $"{tournament.Name} ({tournament.SeasonYear}, {tournament.Sport})",
                Selected = selectedId == tournament.TournamentId
            })
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<SelectListItem>> GetTeamOptionsAsync(
        int? selectedId,
        CancellationToken cancellationToken) =>
        await context.Teams.AsNoTracking()
            .OrderBy(team => team.Name)
            .Select(team => new SelectListItem
            {
                Value = team.TeamId.ToString(),
                Text = $"{team.Name} ({team.Sport})",
                Selected = selectedId == team.TeamId
            })
            .ToListAsync(cancellationToken);

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
