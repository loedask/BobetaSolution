using Bobeta.Application.DTOs.Game;

namespace Bobeta.Application.Interfaces;

/// <summary>Application service for game session lifecycle: create, join, propose/accept bet changes.</summary>
public interface IGameSessionService
{
    /// <summary>Creates a new game with the given bet amount and locks that amount for the creator. Returns the new session DTO.</summary>
    Task<GameSessionDto> CreateGameAsync(Guid playerId, decimal betAmount, CancellationToken cancellationToken = default);

    /// <summary>Joins an existing waiting game; locks bet for the joiner. Returns the session DTO or null if not joinable.</summary>
    Task<GameSessionDto?> JoinGameAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default);

    /// <summary>Proposes a new bet amount for the game (notification/real-time handling can be wired separately).</summary>
    Task ProposeNewBetAsync(Guid playerId, Guid gameId, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>Accepts a pending bet change for the game.</summary>
    Task AcceptBetChangeAsync(Guid gameId, CancellationToken cancellationToken = default);
}
