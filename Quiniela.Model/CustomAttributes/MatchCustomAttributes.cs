using System.ComponentModel.DataAnnotations.Schema;

namespace Quiniela.Model;

public partial class Match
{
    [NotMapped]
    public string ElapsedTimeSinceMatch
    {
        get
        {
            DateTime now = DateTime.UtcNow;
            DateTime kickoff = KickoffAt;

            if (kickoff > now)
            {
                return "0 months, 0 days, 0 hours and 0 minutes";
            }

            int months = (now.Year - kickoff.Year) * 12 + now.Month - kickoff.Month;
            DateTime monthBoundary = kickoff.AddMonths(months);

            if (monthBoundary > now)
            {
                months--;
                monthBoundary = kickoff.AddMonths(months);
            }

            int days = (now - monthBoundary).Days;
            DateTime dayBoundary = monthBoundary.AddDays(days);
            int hours = (now - dayBoundary).Hours;
            DateTime hourBoundary = dayBoundary.AddHours(hours);
            int minutes = (now - hourBoundary).Minutes;

            return $"{months} months, {days} days, {hours} hours and {minutes} minutes";
        }
    }
}
