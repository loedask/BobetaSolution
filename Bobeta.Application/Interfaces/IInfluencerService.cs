using Bobeta.Application.DTOs.Portal;

namespace Bobeta.Application.Interfaces;

public interface IInfluencerService
{
  Task<IReadOnlyList<InfluencerListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
  Task<InfluencerListItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  Task<InfluencerListItemDto?> GetByPortalUserIdAsync(Guid portalUserId, CancellationToken cancellationToken = default);
  Task<InfluencerListItemDto> RegisterAsync(RegisterInfluencerRequest request, Guid createdById, CancellationToken cancellationToken = default);
  Task<InfluencerListItemDto> UpdateCommissionAsync(UpdateInfluencerCommissionRequest request, CancellationToken cancellationToken = default);
  Task<InfluencerRevenueReportDto> GetRevenueReportAsync(Guid influencerId, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken cancellationToken = default);
}
