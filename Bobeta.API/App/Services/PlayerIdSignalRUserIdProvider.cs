using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Bobeta.API.App.Services;

/// <summary>Uses the same player id claim as API controllers so <see cref="IHubClients{T}.User(string)"/> targets the intended account.</summary>
public sealed class PlayerIdSignalRUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst("playerId")?.Value
        ?? connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
