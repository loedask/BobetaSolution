using Bobeta.Application.DTOs.Wallet;

namespace Bobeta.Application.Interfaces;

public interface IWalletService
{
    Task<WalletBalanceDto> GetBalanceAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task<WalletTransactionDto> DepositAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default);
    Task<WalletTransactionDto> WithdrawAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default);
    Task LockBetAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default);
    Task ReleaseBetAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default);
    Task SettleGameAsync(Guid winnerId, Guid loserId, decimal betAmountPerPlayer, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WalletTransactionDto>> GetTransactionsAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default);
}
