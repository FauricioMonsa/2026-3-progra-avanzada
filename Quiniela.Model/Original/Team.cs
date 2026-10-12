using System;
using System.Collections.Generic;

namespace Quiniela.Model;

public partial class Team
{
    public int TeamId { get; set; }

    public string Name { get; set; } = null!;

    public SportType Sport { get; set; }

    public virtual ICollection<MatchParticipant> MatchParticipants { get; set; } = new List<MatchParticipant>();
}
