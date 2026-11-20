using Microsoft.AspNetCore.SignalR;

namespace Quiniela.Api.Hubs;

public sealed class ForecastsHub : Hub
{
    public Task JoinMatch(int matchId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(matchId, 1);
        return Groups.AddToGroupAsync(Context.ConnectionId, GetMatchGroup(matchId));
    }

    public Task LeaveMatch(int matchId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(matchId, 1);
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetMatchGroup(matchId));
    }

    public static string GetMatchGroup(int matchId) => $"match:{matchId}";
}
