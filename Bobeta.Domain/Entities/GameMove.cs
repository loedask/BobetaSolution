namespace Bobeta.Domain.Entities;

/// <summary>
/// A single card play within a game session. Persisted for history and replay.
/// </summary>
public class GameMove
{
    /// <summary>Unique identifier for the move.</summary>
    public Guid Id { get; set; }

    /// <summary>Game session this move belongs to.</summary>
    public Guid GameSessionId { get; set; }

    /// <summary>Player who played the card.</summary>
    public Guid PlayerId { get; set; }

    /// <summary>Zero-based order of this play within the game.</summary>
    public int MoveOrder { get; set; }

    /// <summary>When the move was recorded.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Card played or move notation for history (Makopa Suit_Rank, Domino/Kopo compact markers).</summary>
    public string CardSuitRank { get; set; } = string.Empty;

    /// <summary>Navigation to the game session.</summary>
    public GameSession GameSession { get; set; } = null!;

    /// <summary>Navigation to the player.</summary>
    public Player Player { get; set; } = null!;
}
