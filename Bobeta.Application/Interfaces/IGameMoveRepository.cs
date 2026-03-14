using Bobeta.Domain.Entities;

namespace Bobeta.Application.Interfaces;

public interface IGameMoveRepository
{
    Task<GameMove> AddAsync(GameMove move, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GameMove>> GetByGameSessionIdAsync(Guid gameSessionId, CancellationToken cancellationToken = default);
    Task<int> GetCountByGameSessionIdAsync(Guid gameSessionId, CancellationToken cancellationToken = default);
}
