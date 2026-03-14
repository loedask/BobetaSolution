using Bobeta.Application.DTOs.History;

namespace Bobeta.Application.Interfaces;

/// <summary>Application service for a player's game history (past sessions and results).</summary>
public interface IGameHistoryService
{
    /// <summary>Returns paginated list of game sessions the player participated in, with opponent and result info.</summary>
    Task<IReadOnlyList<GameHistoryItemDto>> GetPlayerHistoryAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default);
}
