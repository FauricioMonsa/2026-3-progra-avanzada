using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Quiniela.Model;

namespace Quiniela.Mvc.Models;

public sealed class MatchFormViewModel
{
    [Display(Name = "Tournament")]
    public int? TournamentId { get; set; }

    [Display(Name = "Round")]
    public string? RoundName { get; set; }

    [Required]
    [Display(Name = "Match kind")]
    public MatchKind Kind { get; set; } = MatchKind.Competition;

    [Required]
    [Display(Name = "Sport")]
    public SportType Sport { get; set; } = SportType.Soccer;

    [Required]
    [Display(Name = "Home team")]
    public int HomeTeamId { get; set; }

    [Required]
    [Display(Name = "Away team")]
    public int AwayTeamId { get; set; }

    [Required]
    [Display(Name = "Kickoff")]
    [DataType(DataType.DateTime)]
    public DateTime KickoffAt { get; set; }

    public IReadOnlyList<SelectListItem> TournamentOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> TeamOptions { get; set; } = [];
}
