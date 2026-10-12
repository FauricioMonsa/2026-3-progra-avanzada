using Quiniela.Model;

namespace Quiniela.BusinessLogic;

/// <summary>
/// Unified record representing match information for query results.
/// </summary>
public record MatchDetails
{
    public string? TournamentName { get; init; }
    public short? TournamentSeason { get; init; }
    public string? TournamentSportName { get; init; }
    public int MatchId { get; init; }
    public MatchKind MatchKind { get; init; }
    public string HomeTeamName { get; init; } = string.Empty;
    public string AwayTeamName { get; init; } = string.Empty;
    public DateTime DatePlayed { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public int? HomeScore { get; init; }
    public int? AwayScore { get; init; }

    public MatchDetails()
    {
    }

    public MatchDetails(
        string? tournamentName,
        short? tournamentSeason,
        string? tournamentSportName,
        int matchId,
        MatchKind matchKind,
        string homeTeamName,
        string awayTeamName,
        DateTime datePlayed,
        string statusName,
        int? homeScore,
        int? awayScore)
    {
        TournamentName = tournamentName;
        TournamentSeason = tournamentSeason;
        TournamentSportName = tournamentSportName;
        MatchId = matchId;
        MatchKind = matchKind;
        HomeTeamName = homeTeamName;
        AwayTeamName = awayTeamName;
        DatePlayed = datePlayed;
        StatusName = statusName;
        HomeScore = homeScore;
        AwayScore = awayScore;
    }

    public MatchDetails(
        string? tournamentName,
        int? tournamentSeason,
        string? tournamentSportName,
        int matchId,
        MatchKind matchKind,
        string homeTeamName,
        string awayTeamName,
        DateTime datePlayed,
        string statusName,
        int? homeScore,
        int? awayScore)
        : this(
            tournamentName,
            (short?)tournamentSeason,
            tournamentSportName,
            matchId,
            matchKind,
            homeTeamName,
            awayTeamName,
            datePlayed,
            statusName,
            homeScore,
            awayScore)
    {
    }

    public MatchDetails(
        string? tournamentName,
        short? tournamentSeason,
        string? tournamentSportName,
        int matchId,
        MatchKind matchKind,
        string homeTeamName,
        string awayTeamName,
        DateOnly datePlayed,
        string statusName,
        int? homeScore,
        int? awayScore)
        : this(
            tournamentName,
            tournamentSeason,
            tournamentSportName,
            matchId,
            matchKind,
            homeTeamName,
            awayTeamName,
            datePlayed.ToDateTime(TimeOnly.MinValue),
            statusName,
            homeScore,
            awayScore)
    {
    }

    public void Deconstruct(
        out string? tournamentName,
        out short? tournamentSeason,
        out string? tournamentSportName,
        out int matchId,
        out MatchKind matchKind,
        out string homeTeamName,
        out string awayTeamName,
        out DateTime datePlayed,
        out string statusName,
        out int? homeScore,
        out int? awayScore)
    {
        tournamentName = TournamentName;
        tournamentSeason = TournamentSeason;
        tournamentSportName = TournamentSportName;
        matchId = MatchId;
        matchKind = MatchKind;
        homeTeamName = HomeTeamName;
        awayTeamName = AwayTeamName;
        datePlayed = DatePlayed;
        statusName = StatusName;
        homeScore = HomeScore;
        awayScore = AwayScore;
    }

    // Convenience alias properties for maximum compatibility
    public MatchKind Kind => MatchKind;
    public string MatchKindName => MatchKind.ToString();
    public int Id => MatchId;
    public string Status => StatusName;
    public DateOnly MatchDay => DateOnly.FromDateTime(DatePlayed);
    public DateOnly Date => DateOnly.FromDateTime(DatePlayed);
}

/// <summary>
/// Alias record for MatchDetails to support alternate naming conventions.
/// </summary>
public record MatchSummary : MatchDetails
{
    public MatchSummary()
    {
    }

    public MatchSummary(
        string? tournamentName,
        short? tournamentSeason,
        string? tournamentSportName,
        int matchId,
        MatchKind matchKind,
        string homeTeamName,
        string awayTeamName,
        DateTime datePlayed,
        string statusName,
        int? homeScore,
        int? awayScore)
        : base(
            tournamentName,
            tournamentSeason,
            tournamentSportName,
            matchId,
            matchKind,
            homeTeamName,
            awayTeamName,
            datePlayed,
            statusName,
            homeScore,
            awayScore)
    {
    }
}
