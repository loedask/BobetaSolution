using Bobeta.Domain.Entities;

namespace Bobeta.Application.Interfaces;

public interface IGameSessionRepository
{
    Task<GameSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GameSession>> GetWaitingSessionsAsync(decimal betAmount, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GameSession>> GetByPlayerIdAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default);
    Task<GameSession> AddAsync(GameSession session, CancellationToken cancellationToken = default);
    Task UpdateAsync(GameSession session, CancellationToken cancellationToken = default);
}
