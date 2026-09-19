using Quiniela.BusinessLogic;

namespace Quiniela.BusinessLogic.Tests;

public class QuinielaScorerTests
{
    private readonly QuinielaScorer scorer = new();

    [Theory]
    [InlineData(2, 1, 2, 1, 5)]
    [InlineData(2, 1, 3, 0, 2)]
    [InlineData(2, 1, 2, 0, 3)]
    [InlineData(2, 1, 3, 1, 3)]
    [InlineData(1, 1, 0, 0, 2)]
    [InlineData(1, 1, 2, 2, 2)]
    [InlineData(1, 1, 1, 2, 0)]
    [InlineData(0, 2, 1, 0, 0)]
    public void CalculatePoints_returns_expected_points(
        int realTeamAScore,
        int realTeamBScore,
        int userTeamAScore,
        int userTeamBScore,
        int expectedPoints)
    {
        int points = scorer.CalculatePoints(realTeamAScore, realTeamBScore, userTeamAScore, userTeamBScore);

        Assert.Equal(expectedPoints, points);
    }

    [Theory]
    [InlineData(-1, 0, 0, 0, "realTeamAScore")]
    [InlineData(0, -1, 0, 0, "realTeamBScore")]
    [InlineData(0, 0, -1, 0, "userTeamAScore")]
    [InlineData(0, 0, 0, -1, "userTeamBScore")]
    public void CalculatePoints_rejects_negative_scores(
        int realTeamAScore,
        int realTeamBScore,
        int userTeamAScore,
        int userTeamBScore,
        string parameterName)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            scorer.CalculatePoints(realTeamAScore, realTeamBScore, userTeamAScore, userTeamBScore));

        Assert.Equal(parameterName, exception.ParamName);
    }
}
