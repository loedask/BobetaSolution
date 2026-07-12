using Bobeta.Domain.Enums;

namespace Bobeta.Domain.Entities;

/// <summary>Ledger entry recording how platform revenue was split with a license partner.</summary>
public class RevenueAllocation
{
  public Guid Id { get; set; }
  public RevenueAllocationSourceType SourceType { get; set; }
  public Guid SourceId { get; set; }
  public string CountryCode { get; set; } = string.Empty;
  public Guid LicensePartnerId { get; set; }
  public decimal GrossPlatformRevenue { get; set; }
  public decimal PartnerSharePercent { get; set; }
  public decimal PartnerAmount { get; set; }
  /// <summary>Total influencer commission deducted from this gross (games sources only).</summary>
  public decimal InfluencerAmount { get; set; }
  public decimal PlatformRetainedAmount { get; set; }
  public string Currency { get; set; } = "XAF";
  public DateTime CreatedAt { get; set; }

  public LicensePartner LicensePartner { get; set; } = null!;
}
