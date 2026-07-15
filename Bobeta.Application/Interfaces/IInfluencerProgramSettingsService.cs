using Bobeta.Application.DTOs.Portal;

namespace Bobeta.Application.Interfaces;

public interface IInfluencerProgramSettingsService
{
  Task<InfluencerProgramSettingsDto> GetAsync(CancellationToken cancellationToken = default);
  Task<InfluencerProgramSettingsDto> UpdateAsync(UpdateInfluencerProgramSettingsRequest request, Guid updatedById, CancellationToken cancellationToken = default);
  Task<decimal> GetPlayerDiscountPercentAsync(CancellationToken cancellationToken = default);
}
