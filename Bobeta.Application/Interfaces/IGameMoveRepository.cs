using Bobeta.Domain.Entities;

namespace Bobeta.Application.Interfaces;

/// <summary>Persistence contract for individual card plays within a game.</summary>
public interface IGameMoveRepository
{
    /// <summary>Records a single card play and returns it.</summary>
    Task<GameMove> AddAsync(GameMove move, CancellationToken cancellationToken = default);

    /// <summary>Gets all moves for a game session in play order.</summary>
    Task<IReadOnlyList<GameMove>> GetByGameSessionIdAsync(Guid gameSessionId, CancellationToken cancellationToken = default);

    /// <summary>Returns the number of moves already played in the session (for assigning next move order).</summary>
    Task<int> GetCountByGameSessionIdAsync(Guid gameSessionId, CancellationToken cancellationToken = default);
}
