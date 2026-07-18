using System.Text.Json;
using Bobeta.Application.Games.Domino;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bobeta.Application.Tests.Games;

public class DominoGameEngineRulesTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Guid _creator = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _opponent = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task ApplyMoveAsync_WhenPlayerEmptiesHand_SettlesWithCommission()
    {
        var state = new DominoGameState
        {
            CreatorHand = ["6-3"],
            OpponentHand = ["5-1", "4-2"],
            Boneyard = [],
            Chain = ["6-6"],
            LeftEnd = 6,
            RightEnd = 6,
            CurrentTurnPlayerId = _creator,
            IsOpening = false
        };

        var wallet = new RecordingWalletService();
        var notifications = new RecordingNotificationService();
        var session = CreateSession(state);
        var sut = CreateEngine(session, wallet, notifications);

        var move = await sut.ApplyMoveAsync(
            _creator, session.Id, DominoRules.ActionPlay, 6, 3, DominoRules.EndRight);

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
    public async Task ApplyMoveAsync_WhenOpponentEmptiesHand_SettlesForOpponent()
    {
        var state = new DominoGameState
        {
            CreatorHand = ["5-1", "4-2"],
            OpponentHand = ["6-3"],
            Boneyard = [],
            Chain = ["6-6"],
            LeftEnd = 6,
            RightEnd = 6,
            CurrentTurnPlayerId = _opponent,
            IsOpening = false
        };

        var wallet = new RecordingWalletService();
        var notifications = new RecordingNotificationService();
        var session = CreateSession(state);
        var sut = CreateEngine(session, wallet, notifications);

        var move = await sut.ApplyMoveAsync(
            _opponent, session.Id, DominoRules.ActionPlay, 6, 3, DominoRules.EndLeft);

        Assert.True(move.IsSuccess);
        Assert.True(move.State!.GameOver);
        Assert.Equal(_opponent, move.State.WinnerPlayerId);
        Assert.Single(wallet.Settlements);
        Assert.Empty(wallet.Releases);
        Assert.Equal(_opponent, session.GameResult!.WinnerPlayerId);
        Assert.Equal(_creator, session.GameResult.LoserPlayerId);
        Assert.Contains(notifications.GameResults, r => r.PlayerId == _opponent && r.Won && r.Amount == 300m);
        Assert.Null(session.GameStateJson);
    }

    [Fact]
    public async Task ApplyMoveAsync_WhenBlockedLowerPipsWins_Settles()
    {
        var state = new DominoGameState
        {
            CreatorHand = ["1-0"],
            OpponentHand = ["5-5"],
            Boneyard = [],
            Chain = ["6-6"],
            LeftEnd = 6,
            RightEnd = 6,
            CurrentTurnPlayerId = _creator,
            IsOpening = false
        };

        var wallet = new RecordingWalletService();
        var notifications = new RecordingNotificationService();
        var session = CreateSession(state);
        var sut = CreateEngine(session, wallet, notifications);

        var move = await sut.ApplyMoveAsync(
            _creator, session.Id, DominoRules.ActionPass, null, null, null);

        Assert.True(move.IsSuccess);
        Assert.True(move.State!.GameOver);
        Assert.False(move.State.IsDraw);
        Assert.Equal(_creator, move.State.WinnerPlayerId);
        Assert.Single(wallet.Settlements);
        Assert.Empty(wallet.Releases);
        Assert.Equal(_creator, session.GameResult!.WinnerPlayerId);
        Assert.Equal(_opponent, session.GameResult.LoserPlayerId);
        Assert.Equal(100m, session.GameResult.PlatformCommission);
        Assert.Contains(notifications.GameResults, r => r.PlayerId == _creator && r.Won && r.Amount == 300m);
        Assert.Null(session.GameStateJson);
    }

    [Theory]
    [InlineData("play", 6, 3, "right", "D:p:6-3:R")]
    [InlineData("play", 6, 6, "left", "D:p:6-6:L")]
    [InlineData("play", 5, 1, null, "D:p:5-1:-")]
    [InlineData("draw", null, null, null, "D:draw")]
    [InlineData("pass", null, null, null, "D:pass")]
    public void FormatMoveMarker_FitsOriginalVarchar20Budget(
        string action, int? high, int? low, string? end, string expected)
    {
        var marker = DominoRules.FormatMoveMarker(action, high, low, end);

        Assert.Equal(expected, marker);
        Assert.True(marker.Length <= 20, $"Marker '{marker}' length {marker.Length} exceeds varchar(20).");
    }

    [Fact]
    public async Task ApplyMoveAsync_WhenBlockedPipTie_ReleasesBothBets()
    {
        var state = new DominoGameState
        {
            CreatorHand = ["2-0"],
            OpponentHand = ["1-1"],
            Boneyard = [],
            Chain = ["6-6"],
            LeftEnd = 6,
            RightEnd = 6,
            CurrentTurnPlayerId = _creator,
            IsOpening = false
        };

        var wallet = new RecordingWalletService();
        var session = CreateSession(state);
        var sut = CreateEngine(session, wallet);

        var move = await sut.ApplyMoveAsync(
            _creator, session.Id, DominoRules.ActionPass, null, null, null);

        Assert.True(move.IsSuccess);
        Assert.True(move.State!.GameOver);
        Assert.True(move.State.IsDraw);
        Assert.Null(move.State.WinnerPlayerId);
        Assert.Equal(2, wallet.Releases.Count);
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
            Variant = GameVariant.Domino,
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
    public async Task ApplyMoveAsync_RejectsPlayWhenMustDraw()
    {
        var state = new DominoGameState
        {
            CreatorHand = ["1-0"],
            OpponentHand = ["5-5"],
            Boneyard = ["3-2"],
            Chain = ["6-6"],
            LeftEnd = 6,
            RightEnd = 6,
            CurrentTurnPlayerId = _creator,
            IsOpening = false
        };

        var session = CreateSession(state);
        var sut = CreateEngine(session, new RecordingWalletService());

        var move = await sut.ApplyMoveAsync(
            _creator, session.Id, DominoRules.ActionPlay, 1, 0, DominoRules.EndLeft);

        Assert.False(move.IsSuccess);
        Assert.Equal(GameStatus.InProgress, session.Status);
    }

    private GameSession CreateSession(DominoGameState state) => new()
    {
        Id = Guid.NewGuid(),
        CreatorPlayerId = _creator,
        OpponentPlayerId = _opponent,
        BetAmount = 200m,
        Variant = GameVariant.Domino,
        Status = GameStatus.InProgress,
        CreatedAt = DateTime.UtcNow,
        GameStateJson = JsonSerializer.Serialize(state, JsonOptions)
    };

    private static DominoGameEngine CreateEngine(
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
            NullLogger<DominoGameEngine>.Instance);
}
