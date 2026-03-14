using Bobeta.Application.DTOs.History;

namespace Bobeta.Application.Interfaces;

public interface IGameHistoryService
{
    Task<IReadOnlyList<GameHistoryItemDto>> GetPlayerHistoryAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default);
}
