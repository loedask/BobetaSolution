using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Bobeta.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Repositories;

public class LicensePartnerRepository(BobetaDbContext db) : ILicensePartnerRepository
{
  public async Task<LicensePartner?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
    await db.LicensePartners
      .Include(p => p.PortalUser)
      .Include(p => p.CountryAssignments)
      .ThenInclude(a => a.RevenueShareRates)
      .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

  public async Task<LicensePartner?> GetByPortalUserIdAsync(Guid portalUserId, CancellationToken cancellationToken = default) =>
    await db.LicensePartners
      .Include(p => p.PortalUser)
      .Include(p => p.CountryAssignments)
      .ThenInclude(a => a.RevenueShareRates)
      .FirstOrDefaultAsync(p => p.PortalUserId == portalUserId, cancellationToken);

  public async Task<IReadOnlyList<LicensePartner>> GetAllAsync(CancellationToken cancellationToken = default) =>
    await db.LicensePartners
      .AsNoTracking()
      .Include(p => p.PortalUser)
      .Include(p => p.CountryAssignments)
      .ThenInclude(a => a.RevenueShareRates)
      .OrderByDescending(p => p.CreatedAt)
      .ToListAsync(cancellationToken);

  public async Task<LicensePartner> AddAsync(LicensePartner partner, CancellationToken cancellationToken = default)
  {
    db.LicensePartners.Add(partner);
    await db.SaveChangesAsync(cancellationToken);
    return partner;
  }

  public async Task UpdateAsync(LicensePartner partner, CancellationToken cancellationToken = default)
  {
    db.LicensePartners.Update(partner);
    await db.SaveChangesAsync(cancellationToken);
  }

  public async Task<LicensePartnerCountryAssignment?> GetActiveAssignmentForCountryAsync(string countryCode, CancellationToken cancellationToken = default)
  {
    var code = countryCode.Trim().ToUpperInvariant();
    return await db.LicensePartnerCountryAssignments
      .AsNoTracking()
      .Include(a => a.LicensePartner)
      .Where(a => a.CountryCode == code && a.IsActive && a.LicensePartner.IsActive)
      .FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<LicensePartnerCountryAssignment?> GetAssignmentAsync(Guid partnerId, string countryCode, CancellationToken cancellationToken = default)
  {
    var code = countryCode.Trim().ToUpperInvariant();
    return await db.LicensePartnerCountryAssignments
      .Include(a => a.RevenueShareRates)
      .FirstOrDefaultAsync(a => a.LicensePartnerId == partnerId && a.CountryCode == code, cancellationToken);
  }

  public async Task<LicensePartnerRevenueShareRate?> GetEffectiveRateAsync(Guid assignmentId, DateTime atUtc, CancellationToken cancellationToken = default) =>
    await db.LicensePartnerRevenueShareRates
      .AsNoTracking()
      .Where(r => r.AssignmentId == assignmentId
                  && r.EffectiveFrom <= atUtc
                  && (r.EffectiveTo == null || r.EffectiveTo > atUtc))
      .OrderByDescending(r => r.EffectiveFrom)
      .FirstOrDefaultAsync(cancellationToken);

  public async Task<LicensePartnerCountryAssignment> AddAssignmentAsync(LicensePartnerCountryAssignment assignment, CancellationToken cancellationToken = default)
  {
    db.LicensePartnerCountryAssignments.Add(assignment);
    await db.SaveChangesAsync(cancellationToken);
    return assignment;
  }

  public async Task<LicensePartnerRevenueShareRate> AddRateAsync(LicensePartnerRevenueShareRate rate, CancellationToken cancellationToken = default)
  {
    db.LicensePartnerRevenueShareRates.Add(rate);
    await db.SaveChangesAsync(cancellationToken);
    return rate;
  }

  public async Task CloseOpenRatesAsync(Guid assignmentId, DateTime effectiveTo, CancellationToken cancellationToken = default)
  {
    var openRates = await db.LicensePartnerRevenueShareRates
      .Where(r => r.AssignmentId == assignmentId && r.EffectiveTo == null)
      .ToListAsync(cancellationToken);

    foreach (var rate in openRates)
      rate.EffectiveTo = effectiveTo;

    await db.SaveChangesAsync(cancellationToken);
  }

  public async Task<RevenueAllocation> AddAllocationAsync(RevenueAllocation allocation, CancellationToken cancellationToken = default)
  {
    db.RevenueAllocations.Add(allocation);
    await db.SaveChangesAsync(cancellationToken);
    return allocation;
  }

  public async Task<bool> AllocationExistsAsync(
      RevenueAllocationSourceType sourceType,
      Guid sourceId,
      CancellationToken cancellationToken = default) =>
    await db.RevenueAllocations
      .AsNoTracking()
      .AnyAsync(a => a.SourceType == sourceType && a.SourceId == sourceId, cancellationToken);

  public async Task<IReadOnlyList<RevenueAllocation>> GetAllocationsAsync(
      Guid licensePartnerId,
      DateTime? fromUtc,
      DateTime? toUtc,
      string? countryCode,
      RevenueAllocationSourceType? sourceType,
      int skip,
      int take,
      CancellationToken cancellationToken = default)
  {
    var query = db.RevenueAllocations
      .AsNoTracking()
      .Where(a => a.LicensePartnerId == licensePartnerId);

    if (fromUtc.HasValue)
      query = query.Where(a => a.CreatedAt >= fromUtc.Value);
    if (toUtc.HasValue)
      query = query.Where(a => a.CreatedAt <= toUtc.Value);
    if (!string.IsNullOrWhiteSpace(countryCode))
    {
      var code = countryCode.Trim().ToUpperInvariant();
      query = query.Where(a => a.CountryCode == code);
    }
    if (sourceType.HasValue)
      query = query.Where(a => a.SourceType == sourceType.Value);

    return await query
      .OrderByDescending(a => a.CreatedAt)
      .Skip(skip)
      .Take(take)
      .ToListAsync(cancellationToken);
  }

  public async Task<decimal> GetMaxActiveRevenueSharePercentAsync(CancellationToken cancellationToken = default)
  {
    var now = DateTime.UtcNow;
    var rates = await db.LicensePartnerRevenueShareRates
      .AsNoTracking()
      .Where(r => r.EffectiveFrom <= now && (r.EffectiveTo == null || r.EffectiveTo > now))
      .Select(r => r.RevenueSharePercent)
      .ToListAsync(cancellationToken);
    return rates.Count == 0 ? 0 : rates.Max();
  }
}
