namespace Bobeta.Domain.Entities;

/// <summary>Time-bounded revenue share for a partner-country assignment. Rate is snapshotted on each transaction.</summary>
public class LicensePartnerRevenueShareRate
{
  public Guid Id { get; set; }
  public Guid AssignmentId { get; set; }
  /// <summary>Partner share of platform commission (0–100).</summary>
  public decimal RevenueSharePercent { get; set; }
  public DateTime EffectiveFrom { get; set; }
  public DateTime? EffectiveTo { get; set; }
  public DateTime CreatedAt { get; set; }
  public Guid? CreatedByPortalUserId { get; set; }

  public LicensePartnerCountryAssignment Assignment { get; set; } = null!;
}
