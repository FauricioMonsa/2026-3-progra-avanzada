namespace Quiniela.BusinessLogic;

public class QuinielaScorer
{
	public int CalculatePoints(int realTeamAScore, int realTeamBScore, int userTeamAScore, int userTeamBScore)
	{
		ValidateScore(realTeamAScore, nameof(realTeamAScore));
		ValidateScore(realTeamBScore, nameof(realTeamBScore));
		ValidateScore(userTeamAScore, nameof(userTeamAScore));
		ValidateScore(userTeamBScore, nameof(userTeamBScore));

		bool exactScore = realTeamAScore == userTeamAScore && realTeamBScore == userTeamBScore;
		if (exactScore)
		{
			return 5;
		}

		bool correctResult = Math.Sign(realTeamAScore - realTeamBScore) == Math.Sign(userTeamAScore - userTeamBScore);
		if (!correctResult)
		{
			return 0;
		}

		return 2 + (realTeamAScore == userTeamAScore || realTeamBScore == userTeamBScore ? 1 : 0);
	}

	private void ValidateScore(int score, string parameterName)
	{
		if (score < 0)
		{
			throw new ArgumentOutOfRangeException(parameterName, "The score must be greater than or equal to zero.");
		}
	}
}
