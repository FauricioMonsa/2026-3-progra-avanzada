using System;
using System.Collections.Generic;

namespace Quiniela.Model;

public partial class Tournament
{
    public int TournamentId { get; set; }

    public string Name { get; set; } = null!;

    public short SeasonYear { get; set; }

    public SportType Sport { get; set; }

    public string TimeZone { get; set; } = null!;
}
