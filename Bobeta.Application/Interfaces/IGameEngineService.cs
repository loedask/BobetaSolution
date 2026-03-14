using Bobeta.Application.DTOs.Game;
using Bobeta.Domain.ValueObjects;

namespace Bobeta.Application.Interfaces;

public interface IGameEngineService
{
    Task StartGameAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<GameStateDto?> PlayCardAsync(Guid playerId, Guid sessionId, Card card, CancellationToken cancellationToken = default);
    Task<GameStateDto?> GetGameStateAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken = default);
}
