using Bobeta.Domain.Entities;

namespace Bobeta.Application.Interfaces;

public interface IInfluencerRepository
{
  Task<Influencer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  Task<Influencer?> GetByPortalUserIdAsync(Guid portalUserId, CancellationToken cancellationToken = default);
  Task<Influencer?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<Influencer>> GetAllAsync(CancellationToken cancellationToken = default);
  Task<Influencer> AddAsync(Influencer influencer, CancellationToken cancellationToken = default);
  Task UpdateAsync(Influencer influencer, CancellationToken cancellationToken = default);
  Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);
  Task<decimal> GetMaxCommissionPercentAsync(CancellationToken cancellationToken = default);

  Task<InfluencerCodeRedemption?> GetRedemptionAsync(Guid influencerId, Guid playerId, CancellationToken cancellationToken = default);
  Task<InfluencerCodeRedemption?> GetPendingRedemptionForPlayerAsync(Guid playerId, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<InfluencerCodeRedemption>> GetRedemptionsForGameAsync(Guid gameSessionId, CancellationToken cancellationToken = default);
  Task<InfluencerCodeRedemption> AddRedemptionAsync(InfluencerCodeRedemption redemption, CancellationToken cancellationToken = default);
  Task UpdateRedemptionAsync(InfluencerCodeRedemption redemption, CancellationToken cancellationToken = default);
  Task DetachRedemptionsFromGameAsync(Guid gameSessionId, CancellationToken cancellationToken = default);

  Task<bool> CommissionAllocationExistsAsync(Guid gameSessionId, CancellationToken cancellationToken = default);
  Task AddCommissionAllocationAsync(InfluencerCommissionAllocation allocation, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<InfluencerCommissionAllocation>> GetCommissionAllocationsAsync(
      Guid influencerId,
      DateTime? fromUtc,
      DateTime? toUtc,
      int skip,
      int take,
      CancellationToken cancellationToken = default);
}
