namespace Bobeta.Domain.Entities;

/// <summary>
/// Record of the outcome of a finished game: winner, loser, pot split, and platform commission.
/// </summary>
public class GameResult
{
    /// <summary>Unique identifier for the result record.</summary>
    public Guid Id { get; set; }

    /// <summary>Game session this result belongs to.</summary>
    public Guid GameSessionId { get; set; }

    /// <summary>Player who won the game.</summary>
    public Guid WinnerPlayerId { get; set; }

    /// <summary>Player who lost the game.</summary>
    public Guid LoserPlayerId { get; set; }

    /// <summary>Total pot (sum of both players' bets).</summary>
    public decimal TotalPot { get; set; }

    /// <summary>Amount credited to the winner (75% of pot after commission).</summary>
    public decimal WinnerAmount { get; set; }

    /// <summary>Platform commission (25% of pot).</summary>
    public decimal PlatformCommission { get; set; }

    /// <summary>Amount allocated to the license partner from platform commission.</summary>
    public decimal PartnerCommission { get; set; }

    /// <summary>License partner that received a revenue share, if any.</summary>
    public Guid? LicensePartnerId { get; set; }

    /// <summary>When the result was recorded.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Navigation to the game session.</summary>
    public GameSession GameSession { get; set; } = null!;

    /// <summary>Navigation to the winner player.</summary>
    public Player WinnerPlayer { get; set; } = null!;

    /// <summary>Navigation to the loser player.</summary>
    public Player LoserPlayer { get; set; } = null!;
}
