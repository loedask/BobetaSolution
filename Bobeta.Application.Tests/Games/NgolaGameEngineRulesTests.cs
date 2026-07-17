using System.Text.Json;
using Bobeta.Application.Games.Ngola;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bobeta.Application.Tests.Games;

public class NgolaGameEngineRulesTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Guid _creator = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _opponent = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task ApplyMoveAsync_WhenOpponentBlockedWithLowerScore_SettlesWithCommission()
    {
        // Creator sows pit 0 (2 seeds) into own pits 1 & 2; opponent has no pit with >= 2 seeds,
        // so the game ends and creator's remaining seeds outscore the opponent.
        var pits = new int[NgolaRules.TotalPits];
        pits[0] = 2;
        var state = new NgolaGameState { CurrentTurnPlayerId = _creator, Pits = pits };

        var wallet = new RecordingWalletService();
        var notifications = new RecordingNotificationService();
        var session = CreateSession(state);
        var sut = CreateEngine(session, wallet, notifications);

        var move = await sut.ApplyMoveAsync(_creator, session.Id, pitIndex: 0);

        Assert.True(move.IsSuccess);
        Assert.True(move.State!.GameOver);
        Assert.False(move.State.IsDraw);
        Assert.Equal(_creator, move.State.WinnerPlayerId);
        Assert.Single(wallet.Settlements);
        Assert.Empty(wallet.Releases);
        Assert.NotNull(session.GameResult);
        Assert.Equal(400m, session.GameResult!.TotalPot);
        Assert.Equal(100m, session.GameResult.PlatformCommission);
        Assert.Equal(300m, session.GameResult.WinnerAmount);
        Assert.Contains(notifications.GameResults, r => r.PlayerId == _creator && r.Won && r.Amount == 300m);
        Assert.Contains(notifications.GameResults, r => r.PlayerId == _opponent && !r.Won && r.Amount == 200m);
        Assert.Null(session.GameStateJson);
    }

    [Fact]
    public async Task ApplyMoveAsync_WhenFinalScoresTie_ReleasesBothBets()
    {
        // After the move both sides finish with 2 seeds each → draw → release bets, no settlement.
        var pits = new int[NgolaRules.TotalPits];
        pits[0] = 2;   // creator pit that will be sown into pits 1 & 2 (own row, no capture)
        pits[8] = 1;   // opponent pit: blocks the opponent (only 1 seed) and counts toward its score
        var state = new NgolaGameState
        {
            CurrentTurnPlayerId = _creator,
            CreatorScore = 0,
            OpponentScore = 1,
            Pits = pits
        };

        var wallet = new RecordingWalletService();
        var session = CreateSession(state);
        var sut = CreateEngine(session, wallet);

        var move = await sut.ApplyMoveAsync(_creator, session.Id, pitIndex: 0);

        Assert.True(move.IsSuccess);
        Assert.True(move.State!.GameOver);
        Assert.True(move.State.IsDraw);
        Assert.Null(move.State.WinnerPlayerId);
        Assert.Equal(2, wallet.Releases.Count);
        Assert.Contains((_creator, 200m), wallet.Releases);
        Assert.Contains((_opponent, 200m), wallet.Releases);
        Assert.Empty(wallet.Settlements);
        Assert.Null(session.GameResult);
        Assert.Null(session.GameStateJson);
    }

    [Fact]
    public async Task GetGameStateAsync_WhenCancelledWithClearedState_ReportsGameOver()
    {
        var session = new GameSession
        {
            Id = Guid.NewGuid(),
            CreatorPlayerId = _creator,
            OpponentPlayerId = _opponent,
            BetAmount = 200m,
            Variant = GameVariant.Ngola,
            Status = GameStatus.Cancelled,
            CreatedAt = DateTime.UtcNow,
            GameStateJson = null
        };
        var sut = CreateEngine(session, new RecordingWalletService());

        var dto = await sut.GetGameStateAsync(session, _creator);

        Assert.NotNull(dto);
        Assert.True(dto!.GameOver);
        Assert.False(dto.IsDraw);
        Assert.Null(dto.WinnerPlayerId);
        Assert.False(dto.WaitingForGameStart);
    }

    [Fact]
    public async Task ApplyMoveAsync_RejectsPitWithFewerThanTwoSeeds()
    {
        var pits = new int[NgolaRules.TotalPits];
        pits[0] = 1;
        var state = new NgolaGameState { CurrentTurnPlayerId = _creator, Pits = pits };

        var session = CreateSession(state);
        var sut = CreateEngine(session, new RecordingWalletService());

        var move = await sut.ApplyMoveAsync(_creator, session.Id, pitIndex: 0);

        Assert.False(move.IsSuccess);
        Assert.Equal(GameStatus.InProgress, session.Status);
    }

    private GameSession CreateSession(NgolaGameState state) => new()
    {
        Id = Guid.NewGuid(),
        CreatorPlayerId = _creator,
        OpponentPlayerId = _opponent,
        BetAmount = 200m,
        Variant = GameVariant.Ngola,
        Status = GameStatus.InProgress,
        CreatedAt = DateTime.UtcNow,
        GameStateJson = JsonSerializer.Serialize(state, JsonOptions)
    };

    private static NgolaGameEngine CreateEngine(
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
            NullLogger<NgolaGameEngine>.Instance);
}
