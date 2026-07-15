using Bobeta.Application.Services;
using Bobeta.Application.Tests.Games;
using Bobeta.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bobeta.Application.Tests.Services;

public sealed class NotificationServiceTests
{
    [Fact]
    public async Task NotifyOpponentJoinedAsync_PersistsAndPublishesOpponentJoined()
    {
        var repo = new InMemoryPlayerNotificationRepository();
        var publisher = new RecordingNotificationRealtimePublisher();
        var sut = CreateSut(repo, publisher);
        var creatorId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        await sut.NotifyOpponentJoinedAsync(creatorId, sessionId, "Berta", 500m);

        Assert.Single(repo.Items);
        var stored = repo.Items[0];
        Assert.Equal(creatorId, stored.PlayerId);
        Assert.Equal(NotificationType.OpponentJoined, stored.Type);
        Assert.Equal("Berta", stored.ActorName);
        Assert.Equal(500m, stored.Amount);
        Assert.Equal(sessionId, stored.RelatedEntityId);
        Assert.Equal($"/game/{sessionId}", stored.DeepLink);
        Assert.False(stored.IsRead);

        Assert.Single(publisher.Published);
        Assert.Equal(creatorId, publisher.Published[0].PlayerId);
        Assert.Equal(stored.Id, publisher.Published[0].Dto.Id);
        Assert.Equal(NotificationType.OpponentJoined, publisher.Published[0].Dto.Type);
    }

    [Theory]
    [InlineData(true, NotificationType.GameWon)]
    [InlineData(false, NotificationType.GameLost)]
    public async Task NotifyGameResultAsync_MapsWinLossAndHistoryDeepLink(bool won, NotificationType expectedType)
    {
        var repo = new InMemoryPlayerNotificationRepository();
        var publisher = new RecordingNotificationRealtimePublisher();
        var sut = CreateSut(repo, publisher);
        var playerId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        await sut.NotifyGameResultAsync(playerId, sessionId, won, 300m);

        Assert.Single(repo.Items);
        Assert.Equal(expectedType, repo.Items[0].Type);
        Assert.Equal("/history", repo.Items[0].DeepLink);
        Assert.Null(repo.Items[0].ActorName);
        Assert.Equal(300m, repo.Items[0].Amount);
        Assert.Single(publisher.Published);
    }

    [Theory]
    [InlineData(true, true, NotificationType.DepositSuccess)]
    [InlineData(true, false, NotificationType.DepositFailed)]
    [InlineData(false, true, NotificationType.WithdrawSuccess)]
    [InlineData(false, false, NotificationType.WithdrawFailed)]
    public async Task NotifyPaymentAsync_MapsDepositWithdrawAndSuccessFailure(
        bool isDeposit,
        bool success,
        NotificationType expectedType)
    {
        var repo = new InMemoryPlayerNotificationRepository();
        var publisher = new RecordingNotificationRealtimePublisher();
        var sut = CreateSut(repo, publisher);
        var playerId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        await sut.NotifyPaymentAsync(playerId, isDeposit, success, 1000m, paymentId);

        Assert.Single(repo.Items);
        Assert.Equal(expectedType, repo.Items[0].Type);
        Assert.Equal("/dashboard", repo.Items[0].DeepLink);
        Assert.Equal(paymentId, repo.Items[0].RelatedEntityId);
        Assert.Equal(1000m, repo.Items[0].Amount);
        Assert.Single(publisher.Published);
    }

    [Fact]
    public async Task SendGameInviteAsync_UsesJoinDeepLink()
    {
        var repo = new InMemoryPlayerNotificationRepository();
        var publisher = new RecordingNotificationRealtimePublisher();
        var sut = CreateSut(repo, publisher);
        var recipientId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        await sut.SendGameInviteAsync(recipientId, sessionId, "Host", 200m);

        Assert.Single(repo.Items);
        Assert.Equal(NotificationType.GameInvite, repo.Items[0].Type);
        Assert.Equal("/join", repo.Items[0].DeepLink);
        Assert.Equal("Host", repo.Items[0].ActorName);
        Assert.Single(publisher.Published);
    }

    [Fact]
    public async Task SendBetProposalAsync_UsesGameDeepLink()
    {
        var repo = new InMemoryPlayerNotificationRepository();
        var publisher = new RecordingNotificationRealtimePublisher();
        var sut = CreateSut(repo, publisher);
        var recipientId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        await sut.SendBetProposalAsync(recipientId, sessionId, 400m);

        Assert.Single(repo.Items);
        Assert.Equal(NotificationType.BetProposal, repo.Items[0].Type);
        Assert.Equal($"/game/{sessionId}", repo.Items[0].DeepLink);
        Assert.Equal(400m, repo.Items[0].Amount);
        Assert.Single(publisher.Published);
    }

    [Fact]
    public async Task GetInboxAsync_ClampsTakeAndSkip()
    {
        var repo = new InMemoryPlayerNotificationRepository();
        var publisher = new RecordingNotificationRealtimePublisher();
        var sut = CreateSut(repo, publisher);
        var playerId = Guid.NewGuid();

        for (var i = 0; i < 5; i++)
        {
            await repo.AddAsync(new Domain.Entities.PlayerNotification
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                Type = NotificationType.DepositSuccess,
                Amount = i,
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        var page = await sut.GetInboxAsync(playerId, skip: -10, take: 0);
        Assert.Single(page);

        var oversized = await sut.GetInboxAsync(playerId, skip: 0, take: 1000);
        Assert.Equal(5, oversized.Count);
        Assert.Equal(0m, oversized[0].Amount);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsUnreadOnlyForPlayer()
    {
        var repo = new InMemoryPlayerNotificationRepository();
        var sut = CreateSut(repo, new RecordingNotificationRealtimePublisher());
        var playerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        await repo.AddAsync(new Domain.Entities.PlayerNotification
        {
            Id = Guid.NewGuid(), PlayerId = playerId, Type = NotificationType.GameWon, IsRead = false, CreatedAt = DateTime.UtcNow
        });
        await repo.AddAsync(new Domain.Entities.PlayerNotification
        {
            Id = Guid.NewGuid(), PlayerId = playerId, Type = NotificationType.GameLost, IsRead = true, CreatedAt = DateTime.UtcNow
        });
        await repo.AddAsync(new Domain.Entities.PlayerNotification
        {
            Id = Guid.NewGuid(), PlayerId = otherId, Type = NotificationType.GameWon, IsRead = false, CreatedAt = DateTime.UtcNow
        });

        Assert.Equal(1, await sut.GetUnreadCountAsync(playerId));
    }

    [Fact]
    public async Task MarkReadAsync_WhenOwnedUnread_SetsIsRead()
    {
        var repo = new InMemoryPlayerNotificationRepository();
        var sut = CreateSut(repo, new RecordingNotificationRealtimePublisher());
        var playerId = Guid.NewGuid();
        var id = Guid.NewGuid();
        await repo.AddAsync(new Domain.Entities.PlayerNotification
        {
            Id = id, PlayerId = playerId, Type = NotificationType.DepositSuccess, IsRead = false, CreatedAt = DateTime.UtcNow
        });

        await sut.MarkReadAsync(playerId, id);

        Assert.True(repo.Items[0].IsRead);
    }

    [Fact]
    public async Task MarkReadAsync_WhenWrongPlayerOrAlreadyRead_DoesNotUpdate()
    {
        var repo = new InMemoryPlayerNotificationRepository();
        var sut = CreateSut(repo, new RecordingNotificationRealtimePublisher());
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var id = Guid.NewGuid();
        await repo.AddAsync(new Domain.Entities.PlayerNotification
        {
            Id = id, PlayerId = ownerId, Type = NotificationType.DepositSuccess, IsRead = true, CreatedAt = DateTime.UtcNow
        });

        await sut.MarkReadAsync(otherId, id);
        await sut.MarkReadAsync(ownerId, id);
        await sut.MarkReadAsync(ownerId, Guid.NewGuid());

        Assert.True(repo.Items[0].IsRead);
        Assert.Single(repo.Items);
    }

    [Fact]
    public async Task MarkAllReadAsync_MarksOnlyCallerUnreadItems()
    {
        var repo = new InMemoryPlayerNotificationRepository();
        var sut = CreateSut(repo, new RecordingNotificationRealtimePublisher());
        var playerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        await repo.AddAsync(new Domain.Entities.PlayerNotification
        {
            Id = Guid.NewGuid(), PlayerId = playerId, Type = NotificationType.GameWon, IsRead = false, CreatedAt = DateTime.UtcNow
        });
        await repo.AddAsync(new Domain.Entities.PlayerNotification
        {
            Id = Guid.NewGuid(), PlayerId = otherId, Type = NotificationType.GameWon, IsRead = false, CreatedAt = DateTime.UtcNow
        });

        await sut.MarkAllReadAsync(playerId);

        Assert.True(repo.Items.Single(n => n.PlayerId == playerId).IsRead);
        Assert.False(repo.Items.Single(n => n.PlayerId == otherId).IsRead);
    }

    [Fact]
    public async Task NotifyPaymentAsync_WhenRepositoryThrows_DoesNotThrow()
    {
        var sut = new NotificationService(
            new ThrowingPlayerNotificationRepository(),
            new RecordingNotificationRealtimePublisher(),
            NullLogger<NotificationService>.Instance);

        var ex = await Record.ExceptionAsync(() =>
            sut.NotifyPaymentAsync(Guid.NewGuid(), isDeposit: true, success: true, 100m));

        Assert.Null(ex);
    }

    private static NotificationService CreateSut(
        InMemoryPlayerNotificationRepository repo,
        RecordingNotificationRealtimePublisher publisher) =>
        new(repo, publisher, NullLogger<NotificationService>.Instance);
}
