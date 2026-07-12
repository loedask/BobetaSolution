using Bobeta.Application.DTOs.Wallet;

namespace Bobeta.Application.Interfaces;

/// <summary>Application service for player wallet: balance, deposit, withdraw, bet lock/release, game settlement.</summary>
public interface IWalletService
{
    /// <summary>Returns current balance and locked balance for the player.</summary>
    Task<WalletBalanceDto> GetBalanceAsync(Guid playerId, CancellationToken cancellationToken = default);

    /// <summary>Credits the player's wallet (e.g. after successful deposit) and returns the transaction record.</summary>
    Task<WalletTransactionDto> DepositAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>Debits the player's wallet and returns the transaction; throws if insufficient balance.</summary>
    Task<WalletTransactionDto> WithdrawAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>Moves amount from balance to locked (e.g. when entering a game or placing a bet).</summary>
    Task LockBetAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>Moves amount from locked back to balance (e.g. game cancelled or bet released).</summary>
    Task ReleaseBetAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>Settles a finished game: unlocks each player's charged amount, winner credited pot minus commission.</summary>
    Task SettleGameAsync(
        Guid winnerId,
        Guid loserId,
        decimal betAmountPerPlayer,
        decimal winnerChargedAmount,
        decimal loserChargedAmount,
        CancellationToken cancellationToken = default);

    /// <summary>Returns paginated transaction history for the player.</summary>
    Task<IReadOnlyList<WalletTransactionDto>> GetTransactionsAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default);
}
