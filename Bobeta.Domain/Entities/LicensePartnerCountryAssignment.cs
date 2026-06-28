namespace Bobeta.Domain.Entities;

/// <summary>Links a license partner to a country they are licensed to operate in.</summary>
public class LicensePartnerCountryAssignment
{
  public Guid Id { get; set; }
  public Guid LicensePartnerId { get; set; }
  /// <summary>ISO 3166-1 alpha-2 (e.g. CG).</summary>
  public string CountryCode { get; set; } = string.Empty;
  public bool IsActive { get; set; } = true;
  public DateTime CreatedAt { get; set; }

  public LicensePartner LicensePartner { get; set; } = null!;
  public ICollection<LicensePartnerRevenueShareRate> RevenueShareRates { get; set; } = [];
}
