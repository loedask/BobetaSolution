using Bobeta.Application.DTOs.Portal;
using Bobeta.Application.Interfaces;

namespace Bobeta.Application.Services;

public sealed class PlayerQueryService(IPlayerRepository players) : IPlayerQueryService
{
  public async Task<(IReadOnlyList<PlayerListItemDto> Items, int TotalCount)> GetPlayersAsync(
      int skip,
      int take,
      string? search = null,
      CancellationToken cancellationToken = default)
  {
    var (items, total) = await players.GetPagedAsync(skip, take, search, cancellationToken);
    var dtos = items.Select(p => new PlayerListItemDto
    {
      Id = p.Id,
      PhoneNumber = p.PhoneNumber,
      PlayerName = p.PlayerName,
      Language = p.Language,
      CreatedAt = p.CreatedAt,
      IsVerified = p.IsVerified,
      Status = p.Status
    }).ToList();

    return (dtos, total);
  }
}
