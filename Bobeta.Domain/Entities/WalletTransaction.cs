using Bobeta.Domain.Enums;

namespace Bobeta.Domain.Entities;

/// <summary>
/// A single credit or debit entry on a player's wallet (deposit, withdrawal, bet lock/release, win, commission).
/// </summary>
public class WalletTransaction
{
    /// <summary>Unique identifier for the transaction.</summary>
    public Guid Id { get; set; }

    /// <summary>Player whose wallet is affected.</summary>
    public Guid PlayerId { get; set; }

    /// <summary>Signed amount (positive for credit, negative for debit).</summary>
    public decimal Amount { get; set; }

    /// <summary>Kind of transaction (e.g. Deposit, Withdrawal, BetLock, Win).</summary>
    public TransactionType Type { get; set; }

    /// <summary>Current status of the transaction.</summary>
    public TransactionStatus Status { get; set; }

    /// <summary>External or internal reference (e.g. payment provider reference).</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>When the transaction was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Navigation to the player.</summary>
    public Player Player { get; set; } = null!;
}
