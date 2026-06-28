using Bobeta.Application.DTOs.Portal;

namespace Bobeta.Application.Interfaces;

public interface IPlayerQueryService
{
  Task<(IReadOnlyList<PlayerListItemDto> Items, int TotalCount)> GetPlayersAsync(
      int skip,
      int take,
      string? search = null,
      CancellationToken cancellationToken = default);

  Task<PlayerDetailDto?> GetPlayerDetailAsync(Guid playerId, CancellationToken cancellationToken = default);
}
