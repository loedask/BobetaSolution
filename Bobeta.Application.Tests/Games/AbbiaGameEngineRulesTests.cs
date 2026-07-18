using System.Text.Json;
using Bobeta.Application.Games.Abbia;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bobeta.Application.Tests.Games;

/// <summary>Regression tests for Abbia throw resolution, settlement, and draw release.</summary>
public class AbbiaGameEngineRulesTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Guid _creator = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _opponent = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task ApplyThrowAsync_WhenBothThrownUnequal_SettlesWithCommission()
    {
        var (sessionId, first, second) = FindSessionWhereScoresDiffer();
        var session = CreateWaitingSession(sessionId);
        var wallet = new RecordingWalletService();
        var notifications = new RecordingNotificationService();
        var sut = CreateEngine(session, wallet, notifications);

        await sut.StartGameAsync(session);

        var firstMove = await sut.ApplyThrowAsync(first, sessionId);
        Assert.True(firstMove.IsSuccess);
        Assert.False(firstMove.State!.GameOver);

        var secondMove = await sut.ApplyThrowAsync(second, sessionId);
        Assert.True(secondMove.IsSuccess);
        Assert.True(secondMove.State!.GameOver);
        Assert.False(secondMove.State.IsDraw);
        Assert.NotNull(secondMove.State.WinnerPlayerId);
        Assert.Single(wallet.Settlements);
        Assert.Empty(wallet.Releases);
        Assert.NotNull(session.GameResult);
        Assert.Equal(400m, session.GameResult!.TotalPot);
        Assert.Equal(100m, session.GameResult.PlatformCommission);
        Assert.Equal(300m, session.GameResult.WinnerAmount);
        Assert.Equal(2, notifications.GameResults.Count);
        Assert.Null(session.GameStateJson);
    }

    [Fact]
    public async Task ApplyThrowAsync_WhenBothThrownEqual_ReleasesBothBets()
    {
        var (sessionId, first, second) = FindSessionWhereScoresTie();
        var session = CreateWaitingSession(sessionId);
        var wallet = new RecordingWalletService();
        var sut = CreateEngine(session, wallet);

        await sut.StartGameAsync(session);

        Assert.True((await sut.ApplyThrowAsync(first, sessionId)).IsSuccess);
        var secondMove = await sut.ApplyThrowAsync(second, sessionId);

        Assert.True(secondMove.IsSuccess);
        Assert.True(secondMove.State!.GameOver);
        Assert.True(secondMove.State.IsDraw);
        Assert.Null(secondMove.State.WinnerPlayerId);
        Assert.Equal(2, wallet.Releases.Count);
        Assert.Contains((_creator, 200m), wallet.Releases);
        Assert.Contains((_opponent, 200m), wallet.Releases);
        Assert.Empty(wallet.Settlements);
        Assert.Null(session.GameResult);
        Assert.Null(session.GameStateJson);
    }

    [Fact]
    public async Task ApplyThrowAsync_WhenOnlyFirstThrow_GameContinues()
    {
        var sessionId = Guid.NewGuid();
        var session = CreateWaitingSession(sessionId);
        var wallet = new RecordingWalletService();
        var sut = CreateEngine(session, wallet);

        await sut.StartGameAsync(session);
        var state = JsonSerializer.Deserialize<AbbiaGameState>(session.GameStateJson!, JsonOptions)!;
        var first = state.CurrentTurnPlayerId!.Value;

        var move = await sut.ApplyThrowAsync(first, sessionId);

        Assert.True(move.IsSuccess);
        Assert.False(move.State!.GameOver);
        Assert.Equal(GameStatus.InProgress, session.Status);
        Assert.Empty(wallet.Settlements);
        Assert.Empty(wallet.Releases);
        Assert.NotNull(session.GameStateJson);
    }

    private (Guid SessionId, Guid First, Guid Second) FindSessionWhereScoresDiffer()
    {
        for (var i = 0; i < 10_000; i++)
        {
            var sessionId = Guid.NewGuid();
            var first = AbbiaRules.CreateInitial(sessionId, _creator, _opponent).CurrentTurnPlayerId!.Value;
            var second = first == _creator ? _opponent : _creator;
            var creatorScore = AbbiaRules.CarvedUpCount(AbbiaRules.FlipTokens(sessionId, _creator, first == _creator ? 0 : 1));
            var opponentScore = AbbiaRules.CarvedUpCount(AbbiaRules.FlipTokens(sessionId, _opponent, first == _opponent ? 0 : 1));
            if (creatorScore != opponentScore)
                return (sessionId, first, second);
        }

        throw new InvalidOperationException("Could not find an Abbia seed with unequal carved-up counts.");
    }

    private (Guid SessionId, Guid First, Guid Second) FindSessionWhereScoresTie()
    {
        for (var i = 0; i < 10_000; i++)
        {
            var sessionId = Guid.NewGuid();
            var first = AbbiaRules.CreateInitial(sessionId, _creator, _opponent).CurrentTurnPlayerId!.Value;
            var second = first == _creator ? _opponent : _creator;
            var creatorScore = AbbiaRules.CarvedUpCount(AbbiaRules.FlipTokens(sessionId, _creator, first == _creator ? 0 : 1));
            var opponentScore = AbbiaRules.CarvedUpCount(AbbiaRules.FlipTokens(sessionId, _opponent, first == _opponent ? 0 : 1));
            if (creatorScore == opponentScore)
                return (sessionId, first, second);
        }

        throw new InvalidOperationException("Could not find an Abbia seed with tied carved-up counts.");
    }

    private GameSession CreateWaitingSession(Guid sessionId) => new()
    {
        Id = sessionId,
        CreatorPlayerId = _creator,
        OpponentPlayerId = _opponent,
        BetAmount = 200m,
        Variant = GameVariant.Abbia,
        Status = GameStatus.Waiting,
        CreatedAt = DateTime.UtcNow
    };

    private static AbbiaGameEngine CreateEngine(
        GameSession session,
        RecordingWalletService wallet,
        INotificationService? notifications = null) =>
        new(
            new InMemoryGameSessionRepository(session),
            new InMemoryGameMoveRepository(),
            new InMemoryGameResultRepository(result => session.GameResult = result),
            wallet,
            new InMemoryPlayerRepository(
                new Player { Id = session.CreatorPlayerId, PlayerName = "Creator" },
                new Player { Id = session.OpponentPlayerId!.Value, PlayerName = "Opponent" }),
            NoOpGameRevenueService.Instance,
            NoOpInfluencerAttributionService.Instance,
            notifications ?? NoOpNotificationService.Instance,
            NullLogger<AbbiaGameEngine>.Instance);
}
