using Bobeta.Application.DTOs.Portal;
using Bobeta.Application.Interfaces;

namespace Bobeta.Application.Services;

public sealed class PlayerQueryService(IPlayerRepository players, IWalletRepository wallets) : IPlayerQueryService
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

  public async Task<PlayerDetailDto?> GetPlayerDetailAsync(Guid playerId, CancellationToken cancellationToken = default)
  {
    var player = await players.GetByIdAsync(playerId, cancellationToken);
    if (player is null)
      return null;

    var wallet = await wallets.GetByPlayerIdAsync(playerId, cancellationToken);

    return new PlayerDetailDto
    {
      Id = player.Id,
      PhoneNumber = player.PhoneNumber,
      PlayerName = player.PlayerName,
      Language = player.Language,
      CreatedAt = player.CreatedAt,
      IsVerified = player.IsVerified,
      Status = player.Status,
      Wallet = wallet is null
          ? null
          : new PlayerWalletSummaryDto
          {
            Balance = wallet.Balance,
            LockedBalance = wallet.LockedBalance,
            UpdatedAt = wallet.UpdatedAt
          }
    };
  }
}
