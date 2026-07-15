using Bobeta.Application.DTOs.Portal;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Services;

public sealed class InfluencerProgramSettingsService(
    IPlatformSettingsRepository settings) : IInfluencerProgramSettingsService
{
  private const decimal DefaultDiscountPercent = 5m;

  public async Task<InfluencerProgramSettingsDto> GetAsync(CancellationToken cancellationToken = default)
  {
    var percent = await GetPlayerDiscountPercentAsync(cancellationToken);
    return new InfluencerProgramSettingsDto { PlayerDiscountPercent = percent };
  }

  public async Task<InfluencerProgramSettingsDto> UpdateAsync(
      UpdateInfluencerProgramSettingsRequest request,
      Guid updatedById,
      CancellationToken cancellationToken = default)
  {
    if (request.PlayerDiscountPercent is < 0 or > 100)
      throw new InvalidOperationException("Player discount must be between 0 and 100.");

    await settings.UpsertAsync(
        PlatformSettingKeys.InfluencerPlayerDiscountPercent,
        request.PlayerDiscountPercent.ToString(System.Globalization.CultureInfo.InvariantCulture),
        updatedById,
        cancellationToken);

    return new InfluencerProgramSettingsDto { PlayerDiscountPercent = request.PlayerDiscountPercent };
  }

  public async Task<decimal> GetPlayerDiscountPercentAsync(CancellationToken cancellationToken = default)
  {
    var setting = await settings.GetAsync(PlatformSettingKeys.InfluencerPlayerDiscountPercent, cancellationToken);
    if (setting is null)
      return DefaultDiscountPercent;

    return decimal.TryParse(
        setting.Value,
        System.Globalization.NumberStyles.Number,
        System.Globalization.CultureInfo.InvariantCulture,
        out var percent)
      ? percent
      : DefaultDiscountPercent;
  }
}
