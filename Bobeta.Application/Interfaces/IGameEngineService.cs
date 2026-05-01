using Bobeta.Application.DTOs.Game;
using Bobeta.Domain.ValueObjects;

namespace Bobeta.Application.Interfaces;

/// <summary>Makopa game engine: start game (deal), play card (server-authoritative), get current state.</summary>
public interface IGameEngineService
{
    /// <summary>Starts the match: deals 6 cards for hand 1, random first leader, best-of-3 hands.</summary>
    Task StartGameAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>Plays a card for the current player. Validates turn and follow-suit rule. Returns updated state or null if invalid.</summary>
    Task<GameStateDto?> PlayCardAsync(Guid playerId, Guid sessionId, Card card, CancellationToken cancellationToken = default);

    /// <summary>Returns the current game state for the requesting player (their hand, last card, whose turn, game over, winner).</summary>
    Task<GameStateDto?> GetGameStateAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken = default);
}
