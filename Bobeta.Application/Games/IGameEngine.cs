using Bobeta.Application.DTOs.Game;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Games;

/// <summary>Variant-specific authoritative game logic for a seated two-player session.</summary>
public interface IGameEngine
{
    GameVariant Variant { get; }

    Task StartGameAsync(GameSession session, CancellationToken cancellationToken = default);

    Task<GameStateDto?> GetGameStateAsync(GameSession session, Guid playerId, CancellationToken cancellationToken = default);
}
