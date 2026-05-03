using Bobeta.Application.DTOs.Game;
using Bobeta.Domain.ValueObjects;

namespace Bobeta.Application.Interfaces;

/// <summary>Makopa game engine: start game (deal), play card (server-authoritative), get current state.</summary>
public interface IGameEngineService
{
    /// <summary>Starts a single-hand match: 4 cards each, stock from remaining deck; random first lead.</summary>
    Task StartGameAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>Plays a card for the current player. Validates turn and follow-suit rule. Returns updated state or null if invalid.</summary>
    Task<GameStateDto?> PlayCardAsync(Guid playerId, Guid sessionId, Card card, CancellationToken cancellationToken = default);

    /// <summary>Responder has no led suit: lead card is added to responder&apos;s hand; leader opens the next trick.</summary>
    Task<GameStateDto?> VoidFollowDrawAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>Returns the current game state for the requesting player (their hand, last card, whose turn, game over, winner).</summary>
    Task<GameStateDto?> GetGameStateAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken = default);
}
