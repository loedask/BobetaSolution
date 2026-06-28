using Bobeta.Application.Common;
using Bobeta.Application.DTOs.Portal;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Services;

public sealed class LicensePartnerService(
    ILicensePartnerRepository partners,
    IPortalUserRepository portalUsers,
    PortalPasswordHasher passwordHasher) : ILicensePartnerService
{
  public async Task<IReadOnlyList<LicensePartnerListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
  {
    var list = await partners.GetAllAsync(cancellationToken);
    return list.Select(MapPartner).ToList();
  }

  public async Task<LicensePartnerListItemDto?> GetByPortalUserIdAsync(Guid portalUserId, CancellationToken cancellationToken = default)
  {
    var partner = await partners.GetByPortalUserIdAsync(portalUserId, cancellationToken);
    return partner is null ? null : MapPartner(partner);
  }

  public async Task<LicensePartnerListItemDto> RegisterAsync(
      RegisterLicensePartnerRequest request,
      Guid createdById,
      CancellationToken cancellationToken = default)
  {
    ValidateRegistration(request);

    var portalEmail = request.PortalEmail.Trim().ToLowerInvariant();
    if (await portalUsers.GetByEmailAsync(portalEmail, cancellationToken) is not null)
      throw new InvalidOperationException("A portal user with this email already exists.");

    var portalUser = new PortalUser
    {
      Id = Guid.NewGuid(),
      Email = portalEmail,
      FirstName = request.FirstName.Trim(),
      LastName = request.LastName.Trim(),
      Role = PortalUserRole.LicensePartner,
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      CreatedById = createdById
    };
    portalUser.PasswordHash = passwordHasher.Hash(portalUser, request.Password);
    await portalUsers.AddAsync(portalUser, cancellationToken);

    var partner = new LicensePartner
    {
      Id = Guid.NewGuid(),
      LegalName = request.LegalName.Trim(),
      ContactEmail = request.ContactEmail.Trim().ToLowerInvariant(),
      PortalUserId = portalUser.Id,
      IsActive = true,
      CreatedAt = DateTime.UtcNow
    };
    await partners.AddAsync(partner, cancellationToken);

    partner.PortalUser = portalUser;
    return MapPartner(partner);
  }

  public async Task<LicensePartnerCountryDto> AssignCountryAsync(
      AssignLicensePartnerCountryRequest request,
      Guid createdById,
      CancellationToken cancellationToken = default)
  {
    if (CountryCatalog.GetByCode(request.CountryCode) is null)
      throw new InvalidOperationException("Unknown country code.");

    if (request.RevenueSharePercent is < 0 or > 100)
      throw new InvalidOperationException("Revenue share must be between 0 and 100.");

    var partner = await partners.GetByIdAsync(request.LicensePartnerId, cancellationToken)
      ?? throw new InvalidOperationException("License partner not found.");

    var countryCode = request.CountryCode.Trim().ToUpperInvariant();
    var existingForCountry = await partners.GetActiveAssignmentForCountryAsync(countryCode, cancellationToken);
    if (existingForCountry is not null && existingForCountry.LicensePartnerId != partner.Id)
      throw new InvalidOperationException($"Country {countryCode} is already assigned to another license partner.");

    var assignment = await partners.GetAssignmentAsync(partner.Id, countryCode, cancellationToken);
    if (assignment is null)
    {
      assignment = new LicensePartnerCountryAssignment
      {
        Id = Guid.NewGuid(),
        LicensePartnerId = partner.Id,
        CountryCode = countryCode,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
      };
      await partners.AddAssignmentAsync(assignment, cancellationToken);
    }
    else if (!assignment.IsActive)
    {
      assignment.IsActive = true;
    }

    var now = DateTime.UtcNow;
    await partners.CloseOpenRatesAsync(assignment.Id, now, cancellationToken);
    await partners.AddRateAsync(new LicensePartnerRevenueShareRate
    {
      Id = Guid.NewGuid(),
      AssignmentId = assignment.Id,
      RevenueSharePercent = request.RevenueSharePercent,
      EffectiveFrom = now,
      CreatedAt = now,
      CreatedByPortalUserId = createdById
    }, cancellationToken);

    assignment = await partners.GetAssignmentAsync(partner.Id, countryCode, cancellationToken)
      ?? throw new InvalidOperationException("Assignment not found after save.");

    return MapCountry(assignment);
  }

  public async Task<LicensePartnerCountryDto> UpdateRevenueShareAsync(
      UpdateLicensePartnerRevenueShareRequest request,
      Guid createdById,
      CancellationToken cancellationToken = default)
  {
    if (request.RevenueSharePercent is < 0 or > 100)
      throw new InvalidOperationException("Revenue share must be between 0 and 100.");

    var partnerList = await partners.GetAllAsync(cancellationToken);
    var assignment = partnerList
      .SelectMany(p => p.CountryAssignments)
      .FirstOrDefault(a => a.Id == request.AssignmentId)
      ?? throw new InvalidOperationException("Country assignment not found.");

    var effectiveFrom = request.EffectiveFrom ?? DateTime.UtcNow;
    await partners.CloseOpenRatesAsync(assignment.Id, effectiveFrom, cancellationToken);
    await partners.AddRateAsync(new LicensePartnerRevenueShareRate
    {
      Id = Guid.NewGuid(),
      AssignmentId = assignment.Id,
      RevenueSharePercent = request.RevenueSharePercent,
      EffectiveFrom = effectiveFrom,
      CreatedAt = DateTime.UtcNow,
      CreatedByPortalUserId = createdById
    }, cancellationToken);

    assignment = (await partners.GetByIdAsync(assignment.LicensePartnerId, cancellationToken))!
      .CountryAssignments.First(a => a.Id == request.AssignmentId);

    return MapCountry(assignment);
  }

  private static void ValidateRegistration(RegisterLicensePartnerRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.LegalName))
      throw new InvalidOperationException("Legal name is required.");
    if (string.IsNullOrWhiteSpace(request.ContactEmail))
      throw new InvalidOperationException("Contact email is required.");
    if (string.IsNullOrWhiteSpace(request.PortalEmail))
      throw new InvalidOperationException("Portal login email is required.");
    if (string.IsNullOrWhiteSpace(request.FirstName))
      throw new InvalidOperationException("First name is required.");
    if (string.IsNullOrWhiteSpace(request.LastName))
      throw new InvalidOperationException("Last name is required.");
    if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
      throw new InvalidOperationException("Password must be at least 8 characters.");
  }

  private static LicensePartnerListItemDto MapPartner(LicensePartner partner) => new()
  {
    Id = partner.Id,
    LegalName = partner.LegalName,
    ContactEmail = partner.ContactEmail,
    PortalEmail = partner.PortalUser?.Email ?? string.Empty,
    IsActive = partner.IsActive,
    CreatedAt = partner.CreatedAt,
    Countries = partner.CountryAssignments.Select(MapCountry).ToList()
  };

  private static LicensePartnerCountryDto MapCountry(LicensePartnerCountryAssignment assignment)
  {
    var now = DateTime.UtcNow;
    var currentRate = assignment.RevenueShareRates
      .Where(r => r.EffectiveFrom <= now && (r.EffectiveTo == null || r.EffectiveTo > now))
      .OrderByDescending(r => r.EffectiveFrom)
      .FirstOrDefault();

    return new LicensePartnerCountryDto
    {
      AssignmentId = assignment.Id,
      CountryCode = assignment.CountryCode,
      CountryName = CountryCatalog.GetByCode(assignment.CountryCode)?.Name ?? assignment.CountryCode,
      IsActive = assignment.IsActive,
      CurrentRevenueSharePercent = currentRate?.RevenueSharePercent
    };
  }
}
