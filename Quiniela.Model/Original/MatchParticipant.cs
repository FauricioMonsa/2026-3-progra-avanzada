using System;
using System.Collections.Generic;

namespace Quiniela.Model;

public partial class MatchParticipant
{
    public int MatchId { get; set; }

    public int TeamId { get; set; }

    public DateOnly MatchDay { get; set; }

    public virtual Match Match { get; set; } = null!;

    public virtual Team Team { get; set; } = null!;
}
