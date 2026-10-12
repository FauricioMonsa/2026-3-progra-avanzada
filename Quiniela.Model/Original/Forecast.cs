using System;
using System.Collections.Generic;

namespace Quiniela.Model;

public partial class Forecast
{
    public int ForecastId { get; set; }

    public int MatchId { get; set; }

    public int UserId { get; set; }

    public int HomeScore { get; set; }

    public int AwayScore { get; set; }

    public DateTime SubmittedAt { get; set; }

    public virtual Match Match { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
