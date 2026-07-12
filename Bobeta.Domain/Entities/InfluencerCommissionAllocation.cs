namespace Bobeta.Domain.Entities;

/// <summary>Ledger entry for an influencer's share of game platform commission.</summary>
public class InfluencerCommissionAllocation
{
  public Guid Id { get; set; }
  public Guid GameSessionId { get; set; }
  public Guid InfluencerId { get; set; }
  public Guid PlayerId { get; set; }
  public decimal GrossPlatformRevenue { get; set; }
  /// <summary>Base used for this influencer (typically G/2).</summary>
  public decimal AttributionBase { get; set; }
  public decimal CommissionPercent { get; set; }
  public decimal InfluencerAmount { get; set; }
  public string Currency { get; set; } = "XAF";
  public DateTime CreatedAt { get; set; }

  public Influencer Influencer { get; set; } = null!;
  public Player Player { get; set; } = null!;
  public GameSession GameSession { get; set; } = null!;
}
