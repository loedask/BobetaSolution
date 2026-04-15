using Bobeta.Application.Interfaces;
using Bobeta.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Bobeta.API.App.Services;

public sealed class GameSessionSignalRNotifier(IHubContext<GameHub> hubContext) : IGameSessionNotifier
{
    public Task NotifySessionAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(GameHub.GroupPrefix + sessionId).SendAsync("GameStarted", cancellationToken: cancellationToken);
}
