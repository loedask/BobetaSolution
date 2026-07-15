namespace Bobeta.Domain.Entities;

/// <summary>Marketing partner who brings players via a unique invite code.</summary>
public class Influencer
{
  public Guid Id { get; set; }
  public string DisplayName { get; set; } = string.Empty;
  public string ContactEmail { get; set; } = string.Empty;
  /// <summary>Unique invite code used in links and manual entry.</summary>
  public string Code { get; set; } = string.Empty;
  /// <summary>Share of platform game commission (percent of gross). Applied per attributed player as I% of G/2.</summary>
  public decimal CommissionPercent { get; set; }
  public Guid PortalUserId { get; set; }
  public bool IsActive { get; set; } = true;
  public DateTime CreatedAt { get; set; }

  public PortalUser PortalUser { get; set; } = null!;
  public ICollection<InfluencerCodeRedemption> Redemptions { get; set; } = [];
}
