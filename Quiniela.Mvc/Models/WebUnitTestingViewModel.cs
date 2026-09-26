using System.ComponentModel.DataAnnotations;

namespace Quiniela.Mvc.Models;

public class WebUnitTestingViewModel
{
    [Display(Name = "Real team A score")]
    [Range(0, int.MaxValue, ErrorMessage = "The score must be greater than or equal to zero.")]
    public int RealTeamAScore { get; set; }

    [Display(Name = "Real team B score")]
    [Range(0, int.MaxValue, ErrorMessage = "The score must be greater than or equal to zero.")]
    public int RealTeamBScore { get; set; }

    [Display(Name = "User team A score")]
    [Range(0, int.MaxValue, ErrorMessage = "The score must be greater than or equal to zero.")]
    public int UserTeamAScore { get; set; }

    [Display(Name = "User team B score")]
    [Range(0, int.MaxValue, ErrorMessage = "The score must be greater than or equal to zero.")]
    public int UserTeamBScore { get; set; }

    public int? TotalPoints { get; set; }
}
