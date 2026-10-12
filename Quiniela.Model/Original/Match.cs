using System;
using System.Collections.Generic;

namespace Quiniela.Model;

public partial class Match
{
    public int MatchId { get; set; }

    public MatchKind Kind { get; set; }

    public int? TournamentId { get; set; }

    public string? RoundName { get; set; }

    public int HomeTeamId { get; set; }

    public int AwayTeamId { get; set; }

    public SportType Sport { get; set; }

    public DateTime KickoffAt { get; set; }

    public string TimeZone { get; set; } = null!;

    public DateOnly MatchDay { get; set; }

    public int? HomeScore { get; set; }

    public int? AwayScore { get; set; }

    public MatchStatus Status { get; set; }

    public string Stats { get; set; } = null!;

    public virtual ICollection<Forecast> Forecasts { get; set; } = new List<Forecast>();

    public virtual ICollection<MatchParticipant> MatchParticipants { get; set; } = new List<MatchParticipant>();
}
