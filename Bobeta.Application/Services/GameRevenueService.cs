using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Services;

public sealed class GameRevenueService(
    IPlayerRepository players,
    IRevenueShareResolver revenueShare,
    IPartnerRevenueAllocationService allocations) : IGameRevenueService
{
  public async Task EnrichWithPartnerShareAsync(GameResult result, Guid winnerPlayerId, CancellationToken cancellationToken = default)
  {
    var player = await players.GetByIdAsync(winnerPlayerId, cancellationToken);
    var countryCode = player?.CountryCode ?? Common.CountryCatalog.ResolveCountryCodeFromPhone(player?.PhoneNumber);
    var split = await revenueShare.ResolveAsync(countryCode, result.PlatformCommission, result.CreatedAt, cancellationToken);

    result.PartnerCommission = split.PartnerAmount;
    result.LicensePartnerId = split.LicensePartnerId;

    await allocations.TryAllocateForPlayerAsync(
        RevenueAllocationSourceType.GameCommission,
        result.GameSessionId,
        winnerPlayerId,
        result.PlatformCommission,
        "XAF",
        result.CreatedAt,
        cancellationToken);
  }
}
