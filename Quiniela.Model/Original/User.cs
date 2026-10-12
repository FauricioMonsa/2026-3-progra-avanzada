using System;
using System.Collections.Generic;

namespace Quiniela.Model;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public virtual ICollection<Forecast> Forecasts { get; set; } = new List<Forecast>();
}
