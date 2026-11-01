using Microsoft.AspNetCore.Mvc.Rendering;
using Quiniela.Model;

namespace Quiniela.Mvc.Models;

public sealed class MatchesIndexViewModel
{
    public IReadOnlyList<MatchMasterViewModel> Matches { get; init; } = [];
    public IReadOnlyList<SelectListItem> TournamentOptions { get; init; } = [];
    public IReadOnlyList<SelectListItem> TeamOptions { get; init; } = [];
    public int? TournamentId { get; init; }
    public int? TeamId { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber * PageSize < TotalCount;
}

public sealed class MatchMasterViewModel
{
    public int MatchId { get; init; }
    public string? TournamentName { get; init; }
    public MatchKind Kind { get; init; }
    public string SportName { get; init; } = string.Empty;
    public string HomeTeamName { get; init; } = string.Empty;
    public string AwayTeamName { get; init; } = string.Empty;
    public DateTime PlayedAt { get; init; }
    public MatchStatus Status { get; init; }
    public int? HomeScore { get; init; }
    public int? AwayScore { get; init; }
    public IReadOnlyList<ForecastDetailViewModel> Forecasts { get; init; } = [];
}

public sealed class ForecastDetailViewModel
{
    public int UserId { get; init; }
    public int PredictedHomeScore { get; init; }
    public int PredictedAwayScore { get; init; }
    public DateTime SubmittedAt { get; init; }
}
