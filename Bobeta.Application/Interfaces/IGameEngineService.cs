using Bobeta.Application.DTOs.Game;
using Bobeta.Domain.ValueObjects;

namespace Bobeta.Application.Interfaces;

/// <summary>Makopa game engine: start game (deal), play card (server-authoritative), get current state.</summary>
public interface IGameEngineService
{
    /// <summary>Starts a single-hand match: 4 cards each from shuffled 52; unused cards are out of play; random first lead.</summary>
    Task StartGameAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>Plays a card for the current player. Validates turn and follow-suit rule.</summary>
    Task<GameMoveResult> PlayCardAsync(Guid playerId, Guid sessionId, Card card, CancellationToken cancellationToken = default);

    /// <summary>Responder has no led suit: lead card is added to responder&apos;s hand; leader opens the next trick.</summary>
    Task<GameMoveResult> VoidFollowDrawAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>Applies a Kopo move path (start + landing squares).</summary>
    Task<GameMoveResult> ApplyKopoMoveAsync(Guid playerId, Guid sessionId, IReadOnlyList<(int Row, int Col)> path, CancellationToken cancellationToken = default);

    /// <summary>Sows all seeds from one of the current player's Ngola pits.</summary>
    Task<GameMoveResult> ApplyNgolaMoveAsync(Guid playerId, Guid sessionId, int pitIndex, CancellationToken cancellationToken = default);

    /// <summary>Applies a Domino play, draw, or pass action.</summary>
    Task<GameMoveResult> ApplyDominoMoveAsync(
        Guid playerId,
        Guid sessionId,
        string action,
        int? high,
        int? low,
        string? end,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the current game state for the requesting player (their hand, last card, whose turn, game over, winner).</summary>
    Task<GameStateDto?> GetGameStateAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken = default);
}
