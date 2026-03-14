namespace Bobeta.Domain.Entities;

/// <summary>
/// Holds a player's real-money balance and any amount locked for active bets.
/// One wallet per player.
/// </summary>
public class Wallet
{
    /// <summary>Unique identifier for the wallet.</summary>
    public Guid Id { get; set; }

    /// <summary>Owner player identifier.</summary>
    public Guid PlayerId { get; set; }

    /// <summary>Available balance that can be withdrawn or used for new bets.</summary>
    public decimal Balance { get; set; }

    /// <summary>Amount currently locked in active game bets.</summary>
    public decimal LockedBalance { get; set; }

    /// <summary>Last time the wallet was updated.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Navigation to the owner player.</summary>
    public Player Player { get; set; } = null!;
}
