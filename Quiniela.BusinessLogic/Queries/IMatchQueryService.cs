namespace Quiniela.BusinessLogic;

public interface IMatchQueryService
{
    /// <summary>
    /// Query 1: List of all matches a team has played on a time interval, sorted chronologically ascending.
    /// </summary>
    Task<List<MatchDetails>> GetMatchesByTeamAsync(
        int teamId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Query 1 (DateOnly overload): List of all matches a team has played on a date interval, sorted chronologically ascending.
    /// </summary>
    Task<List<MatchDetails>> GetMatchesByTeamAsync(
        int teamId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Query 2: List of all matches of a tournament, sorted chronologically ascending.
    /// </summary>
    Task<List<MatchDetails>> GetMatchesByTournamentAsync(
        int tournamentId,
        CancellationToken cancellationToken = default);
}
