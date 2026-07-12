using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Repositories;

public class InfluencerRepository(BobetaDbContext db) : IInfluencerRepository
{
  public async Task<Influencer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
    await db.Influencers
      .Include(i => i.PortalUser)
      .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

  public async Task<Influencer?> GetByPortalUserIdAsync(Guid portalUserId, CancellationToken cancellationToken = default) =>
    await db.Influencers
      .Include(i => i.PortalUser)
      .FirstOrDefaultAsync(i => i.PortalUserId == portalUserId, cancellationToken);

  public async Task<Influencer?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
  {
    var normalized = code.Trim().ToUpperInvariant();
    return await db.Influencers
      .Include(i => i.PortalUser)
      .FirstOrDefaultAsync(i => i.Code == normalized && i.IsActive, cancellationToken);
  }

  public async Task<IReadOnlyList<Influencer>> GetAllAsync(CancellationToken cancellationToken = default) =>
    await db.Influencers
      .AsNoTracking()
      .Include(i => i.PortalUser)
      .OrderByDescending(i => i.CreatedAt)
      .ToListAsync(cancellationToken);

  public async Task<Influencer> AddAsync(Influencer influencer, CancellationToken cancellationToken = default)
  {
    db.Influencers.Add(influencer);
    await db.SaveChangesAsync(cancellationToken);
    return influencer;
  }

  public async Task UpdateAsync(Influencer influencer, CancellationToken cancellationToken = default)
  {
    db.Influencers.Update(influencer);
    await db.SaveChangesAsync(cancellationToken);
  }

  public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
  {
    var normalized = code.Trim().ToUpperInvariant();
    return await db.Influencers.AsNoTracking().AnyAsync(i => i.Code == normalized, cancellationToken);
  }

  public async Task<decimal> GetMaxCommissionPercentAsync(CancellationToken cancellationToken = default)
  {
    var rates = await db.Influencers
      .AsNoTracking()
      .Where(i => i.IsActive)
      .Select(i => i.CommissionPercent)
      .ToListAsync(cancellationToken);
    return rates.Count == 0 ? 0 : rates.Max();
  }

  public async Task<InfluencerCodeRedemption?> GetRedemptionAsync(Guid influencerId, Guid playerId, CancellationToken cancellationToken = default) =>
    await db.InfluencerCodeRedemptions
      .FirstOrDefaultAsync(r => r.InfluencerId == influencerId && r.PlayerId == playerId, cancellationToken);

  public async Task<InfluencerCodeRedemption?> GetPendingRedemptionForPlayerAsync(Guid playerId, CancellationToken cancellationToken = default) =>
    await db.InfluencerCodeRedemptions
      .Include(r => r.Influencer)
      .Where(r => r.PlayerId == playerId && r.ConsumedAt == null && r.GameSessionId == null)
      .OrderByDescending(r => r.AppliedAt)
      .FirstOrDefaultAsync(cancellationToken);

  public async Task<IReadOnlyList<InfluencerCodeRedemption>> GetRedemptionsForGameAsync(Guid gameSessionId, CancellationToken cancellationToken = default) =>
    await db.InfluencerCodeRedemptions
      .Include(r => r.Influencer)
      .Where(r => r.GameSessionId == gameSessionId)
      .ToListAsync(cancellationToken);

  public async Task<InfluencerCodeRedemption> AddRedemptionAsync(InfluencerCodeRedemption redemption, CancellationToken cancellationToken = default)
  {
    db.InfluencerCodeRedemptions.Add(redemption);
    await db.SaveChangesAsync(cancellationToken);
    return redemption;
  }

  public async Task UpdateRedemptionAsync(InfluencerCodeRedemption redemption, CancellationToken cancellationToken = default)
  {
    db.InfluencerCodeRedemptions.Update(redemption);
    await db.SaveChangesAsync(cancellationToken);
  }

  public async Task DetachRedemptionsFromGameAsync(Guid gameSessionId, CancellationToken cancellationToken = default)
  {
    var redemptions = await db.InfluencerCodeRedemptions
      .Where(r => r.GameSessionId == gameSessionId && r.ConsumedAt == null)
      .ToListAsync(cancellationToken);

    foreach (var redemption in redemptions)
    {
      redemption.GameSessionId = null;
      redemption.AttachedAt = null;
    }

    await db.SaveChangesAsync(cancellationToken);
  }

  public async Task<bool> CommissionAllocationExistsAsync(Guid gameSessionId, CancellationToken cancellationToken = default) =>
    await db.InfluencerCommissionAllocations
      .AsNoTracking()
      .AnyAsync(a => a.GameSessionId == gameSessionId, cancellationToken);

  public async Task AddCommissionAllocationAsync(InfluencerCommissionAllocation allocation, CancellationToken cancellationToken = default)
  {
    db.InfluencerCommissionAllocations.Add(allocation);
    await db.SaveChangesAsync(cancellationToken);
  }

  public async Task<IReadOnlyList<InfluencerCommissionAllocation>> GetCommissionAllocationsAsync(
      Guid influencerId,
      DateTime? fromUtc,
      DateTime? toUtc,
      int skip,
      int take,
      CancellationToken cancellationToken = default)
  {
    var query = db.InfluencerCommissionAllocations
      .AsNoTracking()
      .Where(a => a.InfluencerId == influencerId);

    if (fromUtc.HasValue)
      query = query.Where(a => a.CreatedAt >= fromUtc.Value);
    if (toUtc.HasValue)
      query = query.Where(a => a.CreatedAt <= toUtc.Value);

    return await query
      .OrderByDescending(a => a.CreatedAt)
      .Skip(skip)
      .Take(take)
      .ToListAsync(cancellationToken);
  }
}
