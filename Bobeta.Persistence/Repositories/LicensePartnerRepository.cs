using Bobeta.Persistence.Context;
using Bobeta.Domain.Entities;
using Bobeta.Application.Interfaces;
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
}
