using Bobeta.Application.DTOs.Game;
using Bobeta.Application.Interfaces;
using Bobeta.Application.Services;
using Bobeta.Application.Tests.Games;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Xunit;

namespace Bobeta.Application.Tests.Services;

public sealed class GameSessionServiceForfeitTests
{
    [Fact]
    public async Task ForfeitGameAsync_SettlesPotToOpponent_WithCommission()
    {
        var creatorId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        const decimal bet = 200m;
        var session = new GameSession
        {
            Id = sessionId,
            CreatorPlayerId = creatorId,
            OpponentPlayerId = opponentId,
            BetAmount = bet,
            CreatorChargedAmount = bet,
            OpponentChargedAmount = bet,
            Status = GameStatus.InProgress,
            Variant = GameVariant.Makopa,
            CreatedAt = DateTime.UtcNow,
            GameStateJson = "{}"
        };

        var wallet = new RecordingWalletService();
        var notifications = new RecordingNotificationService();
        GameResult? saved = null;
        var sut = CreateSut(session, wallet, notifications, r => saved = r);

        var outcome = await sut.ForfeitGameAsync(creatorId, sessionId);

        Assert.NotNull(outcome);
        Assert.Equal(opponentId, outcome!.WinnerPlayerId);
        Assert.Equal(creatorId, outcome.LoserPlayerId);
        Assert.Equal(300m, outcome.WinnerAmount);
        Assert.Equal(GameStatus.Finished, session.Status);
        Assert.Null(session.GameStateJson);
        Assert.Single(wallet.Settlements);
        Assert.Equal((opponentId, creatorId, bet), wallet.Settlements[0]);
        Assert.Empty(wallet.Releases);
        Assert.NotNull(saved);
        Assert.Equal(opponentId, saved!.WinnerPlayerId);
        Assert.Equal(100m, saved.PlatformCommission);
        Assert.Equal(2, notifications.GameResults.Count);
        Assert.Contains(notifications.GameResults, r => r.PlayerId == opponentId && r.Won && r.Amount == 300m);
        Assert.Contains(notifications.GameResults, r => r.PlayerId == creatorId && !r.Won && r.Amount == bet);
    }

    [Fact]
    public async Task ForfeitGameAsync_WhenOpponentForfeits_SettlesForCreator()
    {
        var creatorId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var session = new GameSession
        {
            Id = Guid.NewGuid(),
            CreatorPlayerId = creatorId,
            OpponentPlayerId = opponentId,
            BetAmount = 100m,
            CreatorChargedAmount = 100m,
            OpponentChargedAmount = 100m,
            Status = GameStatus.InProgress,
            Variant = GameVariant.Kopo,
            CreatedAt = DateTime.UtcNow,
            GameStateJson = "{}"
        };

        var wallet = new RecordingWalletService();
        var sut = CreateSut(session, wallet, new RecordingNotificationService(), _ => { });

        var outcome = await sut.ForfeitGameAsync(opponentId, session.Id);

        Assert.NotNull(outcome);
        Assert.Equal(creatorId, outcome!.WinnerPlayerId);
        Assert.Equal(opponentId, outcome.LoserPlayerId);
        Assert.Equal(150m, outcome.WinnerAmount);
        Assert.Equal((creatorId, opponentId, 100m), wallet.Settlements[0]);
    }

    [Fact]
    public async Task ForfeitGameAsync_WhenNotParticipant_ReturnsNull()
    {
        var session = new GameSession
        {
            Id = Guid.NewGuid(),
            CreatorPlayerId = Guid.NewGuid(),
            OpponentPlayerId = Guid.NewGuid(),
            BetAmount = 100m,
            Status = GameStatus.InProgress,
            Variant = GameVariant.Makopa,
            CreatedAt = DateTime.UtcNow,
            GameStateJson = "{}"
        };
        var sut = CreateSut(session, new RecordingWalletService(), new RecordingNotificationService(), _ => { });

        var outcome = await sut.ForfeitGameAsync(Guid.NewGuid(), session.Id);

        Assert.Null(outcome);
        Assert.Equal(GameStatus.InProgress, session.Status);
    }

    [Fact]
    public async Task ForfeitGameAsync_WhenAlreadyFinished_ReturnsNull()
    {
        var creatorId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var session = new GameSession
        {
            Id = Guid.NewGuid(),
            CreatorPlayerId = creatorId,
            OpponentPlayerId = opponentId,
            BetAmount = 100m,
            Status = GameStatus.Finished,
            Variant = GameVariant.Makopa,
            CreatedAt = DateTime.UtcNow
        };
        var wallet = new RecordingWalletService();
        var sut = CreateSut(session, wallet, new RecordingNotificationService(), _ => { });

        var outcome = await sut.ForfeitGameAsync(creatorId, session.Id);

        Assert.Null(outcome);
        Assert.Empty(wallet.Settlements);
    }

    [Fact]
    public async Task ForfeitGameAsync_WhenWaiting_ReturnsNull()
    {
        var creatorId = Guid.NewGuid();
        var session = new GameSession
        {
            Id = Guid.NewGuid(),
            CreatorPlayerId = creatorId,
            OpponentPlayerId = null,
            BetAmount = 100m,
            Status = GameStatus.Waiting,
            Variant = GameVariant.Makopa,
            CreatedAt = DateTime.UtcNow
        };
        var wallet = new RecordingWalletService();
        var sut = CreateSutWaiting(session, wallet);

        var outcome = await sut.ForfeitGameAsync(creatorId, session.Id);

        Assert.Null(outcome);
        Assert.Empty(wallet.Settlements);
        Assert.Empty(wallet.Releases);
    }

    [Fact]
    public async Task ForfeitGameAsync_DoesNotReleaseBets_UnlikeInactivityCancel()
    {
        var creatorId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var session = new GameSession
        {
            Id = Guid.NewGuid(),
            CreatorPlayerId = creatorId,
            OpponentPlayerId = opponentId,
            BetAmount = 200m,
            CreatorChargedAmount = 200m,
            OpponentChargedAmount = 200m,
            Status = GameStatus.InProgress,
            Variant = GameVariant.Makopa,
            CreatedAt = DateTime.UtcNow,
            GameStateJson = "{}"
        };
        var wallet = new RecordingWalletService();
        var sut = CreateSut(session, wallet, new RecordingNotificationService(), _ => { });

        var outcome = await sut.ForfeitGameAsync(creatorId, session.Id);

        Assert.NotNull(outcome);
        Assert.Single(wallet.Settlements);
        Assert.Empty(wallet.Releases);
        Assert.Equal(GameStatus.Finished, session.Status);
    }

    [Fact]
    public async Task CancelInProgressGameAsync_ReleasesBothBets_WithoutSettlement()
    {
        var creatorId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var session = new GameSession
        {
            Id = Guid.NewGuid(),
            CreatorPlayerId = creatorId,
            OpponentPlayerId = opponentId,
            BetAmount = 200m,
            CreatorChargedAmount = 200m,
            OpponentChargedAmount = 200m,
            Status = GameStatus.InProgress,
            Variant = GameVariant.Makopa,
            CreatedAt = DateTime.UtcNow,
            GameStateJson = "{}"
        };
        var wallet = new RecordingWalletService();
        var sut = CreateSut(session, wallet, new RecordingNotificationService(), _ => { });

        var ok = await sut.CancelInProgressGameAsync(session.Id);

        Assert.True(ok);
        Assert.Empty(wallet.Settlements);
        Assert.Equal(2, wallet.Releases.Count);
        Assert.Equal(GameStatus.Cancelled, session.Status);
    }

    private static GameSessionService CreateSut(
        GameSession session,
        RecordingWalletService wallet,
        RecordingNotificationService notifications,
        Action<GameResult> onResult) =>
        new(
            new InMemoryGameSessionRepository(session),
            new InMemoryPlayerRepository(
                new Player { Id = session.CreatorPlayerId, PlayerName = "Creator" },
                new Player { Id = session.OpponentPlayerId!.Value, PlayerName = "Opponent" }),
            wallet,
            new NoOpGameEngineService(),
            new RecordingGameSessionNotifier(),
            NoOpInfluencerAttributionService.Instance,
            notifications,
            new InMemoryGameResultRepository(onResult),
            NoOpGameRevenueService.Instance);

    private static GameSessionService CreateSutWaiting(GameSession session, RecordingWalletService wallet) =>
        new(
            new InMemoryGameSessionRepository(session),
            new InMemoryPlayerRepository(new Player { Id = session.CreatorPlayerId, PlayerName = "Creator" }),
            wallet,
            new NoOpGameEngineService(),
            new RecordingGameSessionNotifier(),
            NoOpInfluencerAttributionService.Instance,
            new RecordingNotificationService(),
            new InMemoryGameResultRepository(_ => { }),
            NoOpGameRevenueService.Instance);
}
