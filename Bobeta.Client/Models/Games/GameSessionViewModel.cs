namespace Bobeta.Client.Models.Games;

/// <summary>Game session view model. Placeholder for API response.</summary>
public class GameSessionViewModel
{
    public Guid Id { get; set; }
    public Guid CreatorPlayerId { get; set; }
    public Guid? OpponentPlayerId { get; set; }
    public decimal BetAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}
