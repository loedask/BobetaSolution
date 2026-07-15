using Bobeta.Application.DTOs.Portal;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Services;

public sealed class InfluencerService(
    IInfluencerRepository influencers,
    ILicensePartnerRepository partners,
    IPortalUserRepository portalUsers,
    PortalPasswordHasher passwordHasher) : IInfluencerService
{
  public async Task<IReadOnlyList<InfluencerListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
  {
    var list = await influencers.GetAllAsync(cancellationToken);
    return list.Select(Map).ToList();
  }

  public async Task<InfluencerListItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var influencer = await influencers.GetByIdAsync(id, cancellationToken);
    return influencer is null ? null : Map(influencer);
  }

  public async Task<InfluencerListItemDto?> GetByPortalUserIdAsync(Guid portalUserId, CancellationToken cancellationToken = default)
  {
    var influencer = await influencers.GetByPortalUserIdAsync(portalUserId, cancellationToken);
    return influencer is null ? null : Map(influencer);
  }

  public async Task<InfluencerListItemDto> RegisterAsync(
      RegisterInfluencerRequest request,
      Guid createdById,
      CancellationToken cancellationToken = default)
  {
    ValidateRegistration(request);
    await EnsureShareBudgetAsync(request.CommissionPercent, cancellationToken);

    var portalEmail = request.PortalEmail.Trim().ToLowerInvariant();
    if (await portalUsers.GetByEmailAsync(portalEmail, cancellationToken) is not null)
      throw new InvalidOperationException("A portal user with this email already exists.");

    var code = string.IsNullOrWhiteSpace(request.Code)
      ? await GenerateUniqueCodeAsync(cancellationToken)
      : NormalizeCode(request.Code);

    if (await influencers.CodeExistsAsync(code, cancellationToken))
      throw new InvalidOperationException("This invite code is already in use.");

    var portalUser = new PortalUser
    {
      Id = Guid.NewGuid(),
      Email = portalEmail,
      FirstName = request.FirstName.Trim(),
      LastName = request.LastName.Trim(),
      Role = PortalUserRole.Influencer,
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      CreatedById = createdById
    };
    portalUser.PasswordHash = passwordHasher.Hash(portalUser, request.Password);
    await portalUsers.AddAsync(portalUser, cancellationToken);

    var influencer = new Influencer
    {
      Id = Guid.NewGuid(),
      DisplayName = request.DisplayName.Trim(),
      ContactEmail = request.ContactEmail.Trim().ToLowerInvariant(),
      Code = code,
      CommissionPercent = request.CommissionPercent,
      PortalUserId = portalUser.Id,
      IsActive = true,
      CreatedAt = DateTime.UtcNow
    };
    await influencers.AddAsync(influencer, cancellationToken);
    influencer.PortalUser = portalUser;
    return Map(influencer);
  }

  public async Task<InfluencerListItemDto> UpdateCommissionAsync(
      UpdateInfluencerCommissionRequest request,
      CancellationToken cancellationToken = default)
  {
    if (request.CommissionPercent is < 0 or > 100)
      throw new InvalidOperationException("Commission must be between 0 and 100.");

    await EnsureShareBudgetAsync(request.CommissionPercent, cancellationToken);

    var influencer = await influencers.GetByIdAsync(request.InfluencerId, cancellationToken)
      ?? throw new InvalidOperationException("Influencer not found.");

    influencer.CommissionPercent = request.CommissionPercent;
    await influencers.UpdateAsync(influencer, cancellationToken);
    return Map(influencer);
  }

  public async Task<InfluencerRevenueReportDto> GetRevenueReportAsync(
      Guid influencerId,
      DateTime? fromUtc = null,
      DateTime? toUtc = null,
      CancellationToken cancellationToken = default)
  {
    var influencer = await influencers.GetByIdAsync(influencerId, cancellationToken)
      ?? throw new InvalidOperationException("Influencer not found.");

    var allocations = await influencers.GetCommissionAllocationsAsync(
        influencerId, fromUtc, toUtc, skip: 0, take: 10_000, cancellationToken);

    return new InfluencerRevenueReportDto
    {
      InfluencerId = influencer.Id,
      DisplayName = influencer.DisplayName,
      Code = influencer.Code,
      From = fromUtc,
      To = toUtc,
      TotalInfluencerAmount = allocations.Sum(a => a.InfluencerAmount),
      TransactionCount = allocations.Count,
      RecentAllocations = allocations
        .OrderByDescending(a => a.CreatedAt)
        .Take(50)
        .Select(a => new InfluencerRevenueAllocationItemDto
        {
          Id = a.Id,
          GameSessionId = a.GameSessionId,
          PlayerId = a.PlayerId,
          GrossPlatformRevenue = a.GrossPlatformRevenue,
          AttributionBase = a.AttributionBase,
          CommissionPercent = a.CommissionPercent,
          InfluencerAmount = a.InfluencerAmount,
          Currency = a.Currency,
          CreatedAt = a.CreatedAt
        })
        .ToList()
    };
  }

  private async Task EnsureShareBudgetAsync(decimal influencerPercent, CancellationToken cancellationToken)
  {
    if (influencerPercent is < 0 or > 100)
      throw new InvalidOperationException("Commission must be between 0 and 100.");

    var maxPartner = await partners.GetMaxActiveRevenueSharePercentAsync(cancellationToken);
    if (influencerPercent + maxPartner > 100)
      throw new InvalidOperationException(
          $"Influencer commission plus the highest partner share ({maxPartner:N2}%) cannot exceed 100%.");
  }

  private async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
  {
    for (var attempt = 0; attempt < 20; attempt++)
    {
      var code = Convert.ToHexString(Guid.NewGuid().ToByteArray())[..8];
      if (!await influencers.CodeExistsAsync(code, cancellationToken))
        return code;
    }

    throw new InvalidOperationException("Could not generate a unique invite code.");
  }

  private static string NormalizeCode(string code)
  {
    var normalized = code.Trim().ToUpperInvariant();
    if (normalized.Length is < 4 or > 32)
      throw new InvalidOperationException("Invite code must be 4–32 characters.");
    if (normalized.Any(c => !char.IsLetterOrDigit(c)))
      throw new InvalidOperationException("Invite code may only contain letters and digits.");
    return normalized;
  }

  private static void ValidateRegistration(RegisterInfluencerRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.DisplayName))
      throw new InvalidOperationException("Display name is required.");
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

  private static InfluencerListItemDto Map(Influencer influencer) => new()
  {
    Id = influencer.Id,
    DisplayName = influencer.DisplayName,
    ContactEmail = influencer.ContactEmail,
    PortalEmail = influencer.PortalUser?.Email ?? string.Empty,
    Code = influencer.Code,
    CommissionPercent = influencer.CommissionPercent,
    IsActive = influencer.IsActive,
    CreatedAt = influencer.CreatedAt
  };
}
