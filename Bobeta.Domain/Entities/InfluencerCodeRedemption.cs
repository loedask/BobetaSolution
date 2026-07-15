namespace Bobeta.Domain.Entities;

/// <summary>
/// One-time use of an influencer code by a player.
/// Attaches to a single game; if that game is cancelled before finish, the redemption can be re-attached.
/// The same influencer code cannot be applied again by the same player.
/// </summary>
public class InfluencerCodeRedemption
{
  public Guid Id { get; set; }
  public Guid InfluencerId { get; set; }
  public Guid PlayerId { get; set; }
  /// <summary>Snapshot of the code at apply time.</summary>
  public string Code { get; set; } = string.Empty;
  public DateTime AppliedAt { get; set; }
  /// <summary>Game this redemption was used on, if any.</summary>
  public Guid? GameSessionId { get; set; }
  /// <summary>When the code was locked onto a game (create/join).</summary>
  public DateTime? AttachedAt { get; set; }
  /// <summary>When the game finished and commission was finalized (permanent consume).</summary>
  public DateTime? ConsumedAt { get; set; }

  public Influencer Influencer { get; set; } = null!;
  public Player Player { get; set; } = null!;
  public GameSession? GameSession { get; set; }
}
