using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Interfaces;

/// <summary>Persistence contract for game sessions (create, join, lookup, history).</summary>
public interface IGameSessionRepository
{
    /// <summary>Gets a game session by id, including result if loaded, or null if not found.</summary>
    Task<GameSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets waiting sessions (no opponent yet) that match the given bet amount, for join matching.</summary>
    Task<IReadOnlyList<GameSession>> GetWaitingSessionsAsync(decimal betAmount, CancellationToken cancellationToken = default);

    /// <summary>Waiting sessions with no opponent that the given player did not create (join lobby).</summary>
    Task<IReadOnlyList<GameSession>> GetJoinableWaitingSessionsAsync(Guid forPlayerId, int skip, int take, GameVariant? variant = null, CancellationToken cancellationToken = default);

    /// <summary>Gets sessions where the player is creator or opponent, for history; includes result. Ordered by created descending, with paging.</summary>
    Task<IReadOnlyList<GameSession>> GetByPlayerIdAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>True when the player already created a waiting table with no opponent.</summary>
    Task<bool> HasOpenWaitingSeatAsync(Guid playerId, CancellationToken cancellationToken = default);

    /// <summary>True when the player is in a live match (creator or opponent).</summary>
    Task<bool> HasInProgressGameAsync(Guid playerId, CancellationToken cancellationToken = default);

    /// <summary>Creates a new game session and returns it.</summary>
    Task<GameSession> AddAsync(GameSession session, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing game session (e.g. opponent joined, status, game state JSON).</summary>
    Task UpdateAsync(GameSession session, CancellationToken cancellationToken = default);
}
