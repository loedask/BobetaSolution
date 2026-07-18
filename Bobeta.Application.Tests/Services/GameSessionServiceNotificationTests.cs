using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Interfaces;
using Bobeta.Application.Services;
using Bobeta.Application.Tests.Games;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Bobeta.Domain.ValueObjects;
using Xunit;

namespace Bobeta.Application.Tests.Services;

public sealed class GameSessionServiceNotificationTests
{
    [Fact]
    public async Task JoinGameAsync_NotifiesCreatorOpponentJoined()
    {
        var creatorId = Guid.NewGuid();
        var joinerId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var session = new GameSession
        {
            Id = sessionId,
            CreatorPlayerId = creatorId,
            BetAmount = 250m,
            CreatorChargedAmount = 250m,
            Status = GameStatus.Waiting,
            Variant = GameVariant.Makopa,
            CreatedAt = DateTime.UtcNow
        };

        var notifications = new RecordingNotificationService();
        var wallet = new RecordingWalletService();
        var notifier = new RecordingGameSessionNotifier();
        var sut = new GameSessionService(
            new InMemoryGameSessionRepository(session),
            new InMemoryPlayerRepository(
                new Player { Id = creatorId, PlayerName = "Host" },
                new Player { Id = joinerId, PlayerName = "Berta Demo" }),
            wallet,
            new NoOpGameEngineService(),
            notifier,
            NoOpInfluencerAttributionService.Instance,
            notifications);

        var result = await sut.JoinGameAsync(joinerId, sessionId);

        Assert.NotNull(result);
        Assert.Equal(joinerId, session.OpponentPlayerId);
        Assert.Single(wallet.Locks);
        Assert.Equal((joinerId, 250m), wallet.Locks[0]);
        Assert.Single(notifier.Sessions);
        Assert.Equal(sessionId, notifier.Sessions[0]);
        Assert.Single(notifications.OpponentJoined);
        Assert.Equal((creatorId, sessionId, "Berta Demo", 250m), notifications.OpponentJoined[0]);
    }

    [Fact]
    public async Task JoinGameAsync_WhenJoinerHasNoName_UsesOpponentFallback()
    {
        var creatorId = Guid.NewGuid();
        var joinerId = Guid.NewGuid();
        var session = new GameSession
        {
            Id = Guid.NewGuid(),
            CreatorPlayerId = creatorId,
            BetAmount = 200m,
            CreatorChargedAmount = 200m,
            Status = GameStatus.Waiting,
            Variant = GameVariant.Makopa,
            CreatedAt = DateTime.UtcNow
        };
        var notifications = new RecordingNotificationService();
        var sut = new GameSessionService(
            new InMemoryGameSessionRepository(session),
            new InMemoryPlayerRepository(
                new Player { Id = creatorId, PlayerName = "Host" },
                new Player { Id = joinerId, PlayerName = "   " }),
            new RecordingWalletService(),
            new NoOpGameEngineService(),
            new RecordingGameSessionNotifier(),
            NoOpInfluencerAttributionService.Instance,
            notifications);

        await sut.JoinGameAsync(joinerId, session.Id);

        Assert.Single(notifications.OpponentJoined);
        Assert.Equal("Opponent", notifications.OpponentJoined[0].OpponentName);
    }

    [Fact]
    public async Task JoinGameAsync_WhenCreatorJoinsOwnGame_DoesNotNotify()
    {
        var creatorId = Guid.NewGuid();
        var session = new GameSession
        {
            Id = Guid.NewGuid(),
            CreatorPlayerId = creatorId,
            BetAmount = 200m,
            CreatorChargedAmount = 200m,
            Status = GameStatus.Waiting,
            Variant = GameVariant.Makopa,
            CreatedAt = DateTime.UtcNow
        };
        var notifications = new RecordingNotificationService();
        var sut = new GameSessionService(
            new InMemoryGameSessionRepository(session),
            new InMemoryPlayerRepository(new Player { Id = creatorId, PlayerName = "Host" }),
            new RecordingWalletService(),
            new NoOpGameEngineService(),
            new RecordingGameSessionNotifier(),
            NoOpInfluencerAttributionService.Instance,
            notifications);

        await sut.JoinGameAsync(creatorId, session.Id);

        Assert.Empty(notifications.OpponentJoined);
    }

    [Fact]
    public async Task JoinGameAsync_WhenSessionNotJoinable_DoesNotNotify()
    {
        var session = new GameSession
        {
            Id = Guid.NewGuid(),
            CreatorPlayerId = Guid.NewGuid(),
            OpponentPlayerId = Guid.NewGuid(),
            BetAmount = 200m,
            Status = GameStatus.Waiting,
            CreatedAt = DateTime.UtcNow
        };
        var notifications = new RecordingNotificationService();
        var sut = new GameSessionService(
            new InMemoryGameSessionRepository(session),
            new InMemoryPlayerRepository(),
            new RecordingWalletService(),
            new NoOpGameEngineService(),
            new RecordingGameSessionNotifier(),
            NoOpInfluencerAttributionService.Instance,
            notifications);

        var result = await sut.JoinGameAsync(Guid.NewGuid(), session.Id);

        Assert.Null(result);
        Assert.Empty(notifications.OpponentJoined);
    }
}
