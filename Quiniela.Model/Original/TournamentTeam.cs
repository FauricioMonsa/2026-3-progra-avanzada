using System;
using System.Collections.Generic;

namespace Quiniela.Model;

public partial class TournamentTeam
{
    public int TournamentId { get; set; }

    public int TeamId { get; set; }

    public SportType Sport { get; set; }
}
