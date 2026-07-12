namespace Bobeta.Application.DTOs.Portal;

public sealed class LicensePartnerListItemDto
{
  public Guid Id { get; init; }
  public string LegalName { get; init; } = string.Empty;
  public string ContactEmail { get; init; } = string.Empty;
  public string PortalEmail { get; init; } = string.Empty;
  public bool IsActive { get; init; }
  public DateTime CreatedAt { get; init; }
  public IReadOnlyList<LicensePartnerCountryDto> Countries { get; init; } = [];
}

public sealed class LicensePartnerCountryDto
{
  public Guid AssignmentId { get; init; }
  public string CountryCode { get; init; } = string.Empty;
  public string CountryName { get; init; } = string.Empty;
  public bool IsActive { get; init; }
  public decimal? CurrentRevenueSharePercent { get; init; }
}

public sealed class RegisterLicensePartnerRequest
{
  public string LegalName { get; set; } = string.Empty;
  public string ContactEmail { get; set; } = string.Empty;
  public string PortalEmail { get; set; } = string.Empty;
  public string FirstName { get; set; } = string.Empty;
  public string LastName { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
}

public sealed class AssignLicensePartnerCountryRequest
{
  public Guid LicensePartnerId { get; set; }
  public string CountryCode { get; set; } = string.Empty;
  public decimal RevenueSharePercent { get; set; }
}

public sealed class UpdateLicensePartnerRevenueShareRequest
{
  public Guid AssignmentId { get; set; }
  public decimal RevenueSharePercent { get; set; }
  public DateTime? EffectiveFrom { get; set; }
}
