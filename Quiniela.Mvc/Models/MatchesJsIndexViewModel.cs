using Microsoft.AspNetCore.Mvc.Rendering;

namespace Quiniela.Mvc.Models;

public sealed class MatchesJsIndexViewModel
{
    public IReadOnlyList<SelectListItem> TournamentOptions { get; init; } = [];
    public IReadOnlyList<SelectListItem> TeamOptions { get; init; } = [];
}
