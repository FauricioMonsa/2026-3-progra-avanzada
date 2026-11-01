using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Quiniela.Model;

namespace Quiniela.Mvc.Models;

public sealed class MatchEditViewModel
{
    public int MatchId { get; set; }
    public MatchStatus CurrentStatus { get; set; }

    [Required]
    [Display(Name = "New status")]
    public MatchStatus NewStatus { get; set; }

    [Display(Name = "Home score")]
    [Range(0, int.MaxValue)]
    public int? HomeScore { get; set; }

    [Display(Name = "Away score")]
    [Range(0, int.MaxValue)]
    public int? AwayScore { get; set; }

    [Required]
    [Display(Name = "Stats JSON")]
    public string Stats { get; set; } = "{}";

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];
}
