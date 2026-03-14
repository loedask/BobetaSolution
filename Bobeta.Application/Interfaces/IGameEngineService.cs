using Bobeta.Application.DTOs.Game;
using Bobeta.Domain.ValueObjects;

namespace Bobeta.Application.Interfaces;

/// <summary>Makopa game engine: start game (deal), play card (server-authoritative), get current state.</summary>
public interface IGameEngineService
{
    /// <summary>Starts the game: deals 4 cards to each player, sets creator as first lead. Session must be Waiting with opponent set.</summary>
    Task StartGameAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>Plays a card for the current player. Validates turn and follow-suit rule. Returns updated state or null if invalid.</summary>
    Task<GameStateDto?> PlayCardAsync(Guid playerId, Guid sessionId, Card card, CancellationToken cancellationToken = default);

    /// <summary>Returns the current game state for the requesting player (their hand, last card, whose turn, game over, winner).</summary>
    Task<GameStateDto?> GetGameStateAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken = default);
}
