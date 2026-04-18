using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Bobeta.API.Hubs;

/// <summary>SignalR hub for real-time game events: join/leave session, play card, broadcast state and result. Requires JWT (query or header).</summary>
[Authorize]
public class GameHub : Hub
{
    /// <summary>Prefix for group names; group id is GroupPrefix + sessionId.</summary>
    public const string GroupPrefix = "Game_";

    /// <summary>Adds the connection to the group for the given game session (receives broadcasts for that game).</summary>
    public async Task JoinGameSession(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupPrefix + sessionId);
    }

    /// <summary>Removes the connection from the game session group.</summary>
    public async Task LeaveGameSession(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupPrefix + sessionId);
    }

    /// <summary>Notifies others in the session that a card was played (card string format: Suit_Rank).</summary>
    public async Task PlayCard(Guid sessionId, string cardSuitRank)
    {
        await Clients.OthersInGroup(GroupPrefix + sessionId).SendAsync("NotifyOpponentMove", cardSuitRank);
    }

    /// <summary>Broadcasts the current game state to all connections in the session.</summary>
    public async Task BroadcastGameState(Guid sessionId, object gameState)
    {
        await Clients.Group(GroupPrefix + sessionId).SendAsync("GameState", gameState);
    }

    /// <summary>Sends the game result to all connections in the session.</summary>
    public async Task SendGameResult(Guid sessionId, object result)
    {
        await Clients.Group(GroupPrefix + sessionId).SendAsync("GameResult", result);
    }
}
