using System.Globalization;
using Quiniela.BusinessLogic;

namespace Quiniela.AppConsole;

public static class Program
{
	public static int Main(string[] args)
	{
		if (args.Length != 4 || !args.All(argument => int.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)))
		{
			Console.Error.WriteLine("Uso: Quiniela.AppConsole <realA> <realB> <usuarioA> <usuarioB>");
			return 1;
		}

		int realTeamAScore = int.Parse(args[0], CultureInfo.InvariantCulture);
		int realTeamBScore = int.Parse(args[1], CultureInfo.InvariantCulture);
		int userTeamAScore = int.Parse(args[2], CultureInfo.InvariantCulture);
		int userTeamBScore = int.Parse(args[3], CultureInfo.InvariantCulture);

		if (realTeamAScore < 0 || realTeamBScore < 0 || userTeamAScore < 0 || userTeamBScore < 0)
		{
			Console.Error.WriteLine("Los marcadores deben ser enteros mayores o iguales que cero.");
			return 1;
		}

		QuinielaScorer scorer = new();
		int points = scorer.CalculatePoints(realTeamAScore, realTeamBScore, userTeamAScore, userTeamBScore);
		Console.WriteLine($"Puntos: {points}");
		return 0;
	}
}
