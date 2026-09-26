using Microsoft.AspNetCore.Mvc;
using Quiniela.BusinessLogic;
using Quiniela.Mvc.Models;

namespace Quiniela.Mvc.Controllers;

public class QuinielaController(QuinielaScorer scorer) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult WebUnitTesting()
    {
        return View(new WebUnitTestingViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CalculatePoints(WebUnitTestingViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(WebUnitTesting), model);
        }

        model.TotalPoints = scorer.CalculatePoints(
            model.RealTeamAScore,
            model.RealTeamBScore,
            model.UserTeamAScore,
            model.UserTeamBScore);

        return View(nameof(WebUnitTesting), model);
    }
}
