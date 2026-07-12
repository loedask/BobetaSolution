using Bobeta.Application.DTOs.Wallet;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;

namespace Bobeta.Application.Services;

/// <summary>Application service for wallet operations: balance, deposit, withdraw, bet lock/release, and game settlement.</summary>
public class WalletService : IWalletService
{
    private readonly IWalletRepository _walletRepository;
    private readonly IWalletTransactionRepository _transactionRepository;

    public WalletService(
        IWalletRepository walletRepository,
        IWalletTransactionRepository transactionRepository)
    {
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<WalletBalanceDto> GetBalanceAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByPlayerIdAsync(playerId, cancellationToken)
            ?? throw new InvalidOperationException("Wallet not found.");
        return new WalletBalanceDto(wallet.Balance, wallet.LockedBalance);
    }

    public async Task<WalletTransactionDto> DepositAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByPlayerIdAsync(playerId, cancellationToken)
            ?? throw new InvalidOperationException("Wallet not found.");
        wallet.Balance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;
        await _walletRepository.UpdateAsync(wallet, cancellationToken);

        var tx = new WalletTransaction
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            Amount = amount,
            Type = TransactionType.Deposit,
            Status = TransactionStatus.Completed,
            Reference = Guid.NewGuid().ToString("N")[..16],
            CreatedAt = DateTime.UtcNow
        };
        await _transactionRepository.AddAsync(tx, cancellationToken);
        return Map(tx);
    }

    public async Task<WalletTransactionDto> WithdrawAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByPlayerIdAsync(playerId, cancellationToken)
            ?? throw new InvalidOperationException("Wallet not found.");
        if (wallet.Balance < amount)
            throw new InvalidOperationException("Insufficient balance.");
        wallet.Balance -= amount;
        wallet.UpdatedAt = DateTime.UtcNow;
        await _walletRepository.UpdateAsync(wallet, cancellationToken);

        var tx = new WalletTransaction
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            Amount = -amount,
            Type = TransactionType.Withdrawal,
            Status = TransactionStatus.Completed,
            Reference = Guid.NewGuid().ToString("N")[..16],
            CreatedAt = DateTime.UtcNow
        };
        await _transactionRepository.AddAsync(tx, cancellationToken);
        return Map(tx);
    }

    public async Task LockBetAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByPlayerIdAsync(playerId, cancellationToken)
            ?? throw new InvalidOperationException("Wallet not found.");
        if (wallet.Balance < amount)
            throw new InvalidOperationException("Insufficient balance for bet.");
        wallet.Balance -= amount;
        wallet.LockedBalance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;
        await _walletRepository.UpdateAsync(wallet, cancellationToken);

        var tx = new WalletTransaction
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            Amount = -amount,
            Type = TransactionType.BetLock,
            Status = TransactionStatus.Completed,
            Reference = Guid.NewGuid().ToString("N")[..16],
            CreatedAt = DateTime.UtcNow
        };
        await _transactionRepository.AddAsync(tx, cancellationToken);
    }

    public async Task ReleaseBetAsync(Guid playerId, decimal amount, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByPlayerIdAsync(playerId, cancellationToken)
            ?? throw new InvalidOperationException("Wallet not found.");
        if (wallet.LockedBalance < amount)
            throw new InvalidOperationException("Insufficient locked balance.");
        wallet.LockedBalance -= amount;
        wallet.Balance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;
        await _walletRepository.UpdateAsync(wallet, cancellationToken);

        var tx = new WalletTransaction
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            Amount = amount,
            Type = TransactionType.BetRelease,
            Status = TransactionStatus.Completed,
            Reference = Guid.NewGuid().ToString("N")[..16],
            CreatedAt = DateTime.UtcNow
        };
        await _transactionRepository.AddAsync(tx, cancellationToken);
    }

    public async Task SettleGameAsync(
        Guid winnerId,
        Guid loserId,
        decimal betAmountPerPlayer,
        decimal winnerChargedAmount,
        decimal loserChargedAmount,
        CancellationToken cancellationToken = default)
    {
        const decimal commissionRate = 0.25m;
        var totalPot = betAmountPerPlayer * 2;
        var commission = totalPot * commissionRate;
        var winnerAmount = totalPot - commission;

        var winnerWallet = await _walletRepository.GetByPlayerIdAsync(winnerId, cancellationToken) ?? throw new InvalidOperationException("Wallet not found.");
        var loserWallet = await _walletRepository.GetByPlayerIdAsync(loserId, cancellationToken) ?? throw new InvalidOperationException("Wallet not found.");
        if (winnerWallet.LockedBalance < winnerChargedAmount || loserWallet.LockedBalance < loserChargedAmount)
            throw new InvalidOperationException("Insufficient locked balance for settlement.");
        winnerWallet.LockedBalance -= winnerChargedAmount;
        winnerWallet.Balance += winnerAmount;
        winnerWallet.UpdatedAt = DateTime.UtcNow;
        loserWallet.LockedBalance -= loserChargedAmount;
        loserWallet.UpdatedAt = DateTime.UtcNow;
        await _walletRepository.UpdateAsync(winnerWallet, cancellationToken);
        await _walletRepository.UpdateAsync(loserWallet, cancellationToken);

        await _transactionRepository.AddAsync(new WalletTransaction
        {
            Id = Guid.NewGuid(),
            PlayerId = winnerId,
            Amount = winnerAmount,
            Type = TransactionType.Win,
            Status = TransactionStatus.Completed,
            Reference = Guid.NewGuid().ToString("N")[..16],
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<WalletTransactionDto>> GetTransactionsAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var list = await _transactionRepository.GetByPlayerIdAsync(playerId, skip, take, cancellationToken);
        return list.Select(Map).ToList();
    }

    private static WalletTransactionDto Map(WalletTransaction t) =>
        new(t.Id, t.Amount, t.Type, t.Status, t.Reference, t.CreatedAt);
}
