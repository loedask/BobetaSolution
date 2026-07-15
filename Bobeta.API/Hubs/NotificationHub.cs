using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Bobeta.API.Hubs;

/// <summary>SignalR hub for player inbox notifications. Targets users via playerId claim.</summary>
[Authorize]
public class NotificationHub : Hub
{
}
