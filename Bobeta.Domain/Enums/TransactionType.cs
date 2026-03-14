namespace Bobeta.Domain.Enums;

/// <summary>Type of wallet transaction for audit and reporting.</summary>
public enum TransactionType
{
    /// <summary>Player added funds (e.g. via Mobile Money).</summary>
    Deposit = 0,

    /// <summary>Player withdrew funds.</summary>
    Withdrawal = 1,

    /// <summary>Funds locked when entering or placing a bet.</summary>
    BetLock = 2,

    /// <summary>Funds unlocked (e.g. bet cancelled or game not started).</summary>
    BetRelease = 3,

    /// <summary>Winnings credited after winning a game.</summary>
    Win = 4,

    /// <summary>Platform commission taken from the pot.</summary>
    Commission = 5
}
