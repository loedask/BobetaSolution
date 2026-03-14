namespace Bobeta.Domain.Entities;

public class GameMove
{
    public Guid Id { get; set; }
    public Guid GameSessionId { get; set; }
    public Guid PlayerId { get; set; }
    public int MoveOrder { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Stored as "Suit_Rank" e.g. "Heart_Ace" for EF persistence.</summary>
    public string CardSuitRank { get; set; } = string.Empty;

    public GameSession GameSession { get; set; } = null!;
    public Player Player { get; set; } = null!;
}
