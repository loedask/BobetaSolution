using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Bobeta.API.Hubs;

[Authorize]
public class GameHub : Hub
{
    public const string GroupPrefix = "Game_";

    public async Task JoinGameSession(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupPrefix + sessionId);
    }

    public async Task LeaveGameSession(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupPrefix + sessionId);
    }

    public async Task PlayCard(Guid sessionId, string cardSuitRank)
    {
        await Clients.OthersInGroup(GroupPrefix + sessionId).SendAsync("NotifyOpponentMove", cardSuitRank);
    }

    public async Task BroadcastGameState(Guid sessionId, object gameState)
    {
        await Clients.Group(GroupPrefix + sessionId).SendAsync("GameState", gameState);
    }

    public async Task SendGameResult(Guid sessionId, object result)
    {
        await Clients.Group(GroupPrefix + sessionId).SendAsync("GameResult", result);
    }
}
