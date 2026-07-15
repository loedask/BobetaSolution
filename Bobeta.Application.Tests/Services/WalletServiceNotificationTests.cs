using Bobeta.Application.Services;
using Bobeta.Application.Tests.Games;
using Bobeta.Domain.Entities;
using Xunit;

namespace Bobeta.Application.Tests.Services;

public sealed class WalletServiceNotificationTests
{
    [Fact]
    public async Task DepositAsync_NotifiesDepositSuccessWithTransactionId()
    {
        var playerId = Guid.NewGuid();
        var walletRepo = new InMemoryWalletRepository(new Wallet
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            Balance = 0m,
            LockedBalance = 0m,
            UpdatedAt = DateTime.UtcNow
        });
        var txRepo = new InMemoryWalletTransactionRepository();
        var notifications = new RecordingNotificationService();
        var sut = new WalletService(walletRepo, txRepo, notifications);

        var tx = await sut.DepositAsync(playerId, 1500m);

        Assert.Single(notifications.Payments);
        var payment = notifications.Payments[0];
        Assert.Equal(playerId, payment.PlayerId);
        Assert.True(payment.IsDeposit);
        Assert.True(payment.Success);
        Assert.Equal(1500m, payment.Amount);
        Assert.Equal(tx.Id, payment.PaymentTransactionId);
    }

    [Fact]
    public async Task WithdrawAsync_NotifiesWithdrawSuccessWithTransactionId()
    {
        var playerId = Guid.NewGuid();
        var walletRepo = new InMemoryWalletRepository(new Wallet
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            Balance = 2000m,
            LockedBalance = 0m,
            UpdatedAt = DateTime.UtcNow
        });
        var txRepo = new InMemoryWalletTransactionRepository();
        var notifications = new RecordingNotificationService();
        var sut = new WalletService(walletRepo, txRepo, notifications);

        var tx = await sut.WithdrawAsync(playerId, 400m);

        Assert.Single(notifications.Payments);
        var payment = notifications.Payments[0];
        Assert.Equal(playerId, payment.PlayerId);
        Assert.False(payment.IsDeposit);
        Assert.True(payment.Success);
        Assert.Equal(400m, payment.Amount);
        Assert.Equal(tx.Id, payment.PaymentTransactionId);
    }

    [Fact]
    public async Task LockBetAsync_DoesNotNotifyPayment()
    {
        var playerId = Guid.NewGuid();
        var walletRepo = new InMemoryWalletRepository(new Wallet
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            Balance = 1000m,
            LockedBalance = 0m,
            UpdatedAt = DateTime.UtcNow
        });
        var notifications = new RecordingNotificationService();
        var sut = new WalletService(walletRepo, new InMemoryWalletTransactionRepository(), notifications);

        await sut.LockBetAsync(playerId, 200m);

        Assert.Empty(notifications.Payments);
    }

    [Fact]
    public async Task WithdrawAsync_WhenInsufficientBalance_DoesNotNotify()
    {
        var playerId = Guid.NewGuid();
        var walletRepo = new InMemoryWalletRepository(new Wallet
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            Balance = 50m,
            LockedBalance = 0m,
            UpdatedAt = DateTime.UtcNow
        });
        var notifications = new RecordingNotificationService();
        var sut = new WalletService(walletRepo, new InMemoryWalletTransactionRepository(), notifications);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.WithdrawAsync(playerId, 100m));
        Assert.Empty(notifications.Payments);
    }
}
