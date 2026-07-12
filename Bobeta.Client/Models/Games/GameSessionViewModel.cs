using Bobeta.Client.Models.Api;

namespace Bobeta.Client.Models.Games;

/// <summary>Game session view model.</summary>
public class GameSessionViewModel
{
    public Guid Id { get; set; }
    public Guid CreatorPlayerId { get; set; }
    public Guid? OpponentPlayerId { get; set; }
    public decimal BetAmount { get; set; }
    public GameVariant Variant { get; set; }
    public string VariantName => GameVariantLabels.Name(Variant);
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}
