using Microsoft.AspNetCore.Mvc.Rendering;

namespace Quiniela.Mvc.Models;

public sealed class ForecastsChartViewModel
{
    public IReadOnlyList<SelectListItem> SportOptions { get; init; } = [];
    public string HubUrl { get; init; } = string.Empty;
}

public sealed record ForecastChartTeamOption(int TeamId, string Name);

public sealed record ForecastChartMatchOption(
    int MatchId,
    string Label,
    DateTime KickoffAt,
    string TimeZone,
    string Status);
