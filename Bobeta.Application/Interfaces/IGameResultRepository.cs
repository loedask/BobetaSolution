using Bobeta.Domain.Entities;

namespace Bobeta.Application.Interfaces;

public interface IGameResultRepository
{
    Task<GameResult> AddAsync(GameResult result, CancellationToken cancellationToken = default);
}
