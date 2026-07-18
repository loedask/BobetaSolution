using Bobeta.Application.DTOs.Game;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Interfaces;

/// <summary>Application service for game session lifecycle: create, join, propose/accept bet changes.</summary>
public interface IGameSessionService
{
    /// <summary>Creates a new game with the given bet amount and locks that amount for the creator. Returns the new session DTO.</summary>
    Task<GameSessionDto> CreateGameAsync(Guid playerId, decimal betAmount, GameVariant variant = GameVariant.Makopa, CancellationToken cancellationToken = default);

    /// <summary>Joins an existing waiting game; locks bet for the joiner. Returns the session DTO or null if not joinable.</summary>
    Task<GameSessionDto?> JoinGameAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default);

    /// <summary>Lists waiting games the player can join (not created by this player, no opponent yet).</summary>
    Task<IReadOnlyList<GameSessionDto>> ListOpenJoinableGamesAsync(Guid playerId, int skip = 0, int take = 50, GameVariant? variant = null, CancellationToken cancellationToken = default);

    /// <summary>Lists waiting tables this player created that still need an opponent.</summary>
    Task<IReadOnlyList<GameSessionDto>> ListMyWaitingGamesAsync(Guid playerId, int skip = 0, int take = 50, GameVariant? variant = null, CancellationToken cancellationToken = default);

    /// <summary>Proposes a new bet amount for the game (notification/real-time handling can be wired separately).</summary>
    Task ProposeNewBetAsync(Guid playerId, Guid gameId, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>Accepts a pending bet change for the game.</summary>
    Task AcceptBetChangeAsync(Guid gameId, CancellationToken cancellationToken = default);

    /// <summary>Cancels an in-progress match and releases both players&apos; locked stakes.</summary>
    Task<bool> CancelInProgressGameAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Forfeits an in-progress match for <paramref name="loserPlayerId"/>: opponent wins and receives the settled pot.
    /// Returns null if the session is not forfeitable by that player.
    /// </summary>
    Task<ForfeitGameOutcome?> ForfeitGameAsync(Guid loserPlayerId, Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>Cancels a waiting game created by the player and releases their locked stake / invite code.</summary>
    Task<bool> CancelWaitingGameAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken = default);
}
