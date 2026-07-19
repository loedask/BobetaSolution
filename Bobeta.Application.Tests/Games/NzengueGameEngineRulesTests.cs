using System.Text.Json;
using Bobeta.Application.Games.Nzengue;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bobeta.Application.Tests.Games;

/// <summary>Regression tests for Nzengué place/move resolution and settlement.</summary>
public class NzengueGameEngineRulesTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Guid _creator = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _opponent = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task ApplyMoveAsync_PlaceThenWin_SettlesWithCommission()
    {
        var sessionId = Guid.NewGuid();
        var session = CreateWaitingSession(sessionId);
        var wallet = new RecordingWalletService();
        var notifications = new RecordingNotificationService();
        var sut = CreateEngine(session, wallet, notifications);

        await sut.StartGameAsync(session);
        var state = JsonSerializer.Deserialize<NzengueGameState>(session.GameStateJson!, JsonOptions)!;
        // Force a near-win for creator: two on a line, one to place.
        state.Points[0] = _creator;
        state.Points[1] = _creator;
        state.Points[3] = _opponent;
        state.Points[4] = _opponent;
        state.CreatorPiecesToPlace = 1;
        state.OpponentPiecesToPlace = 1;
        state.CurrentTurnPlayerId = _creator;
        session.GameStateJson = JsonSerializer.Serialize(state, JsonOptions);

        var move = await sut.ApplyMoveAsync(_creator, sessionId, fromPoint: null, toPoint: 2);

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
        Assert.Equal(2, notifications.GameResults.Count);
        Assert.Null(session.GameStateJson);
    }

    [Fact]
    public async Task ApplyMoveAsync_PlaceWithoutWin_Continues()
    {
        var sessionId = Guid.NewGuid();
        var session = CreateWaitingSession(sessionId);
        var wallet = new RecordingWalletService();
        var sut = CreateEngine(session, wallet);

        await sut.StartGameAsync(session);
        var state = JsonSerializer.Deserialize<NzengueGameState>(session.GameStateJson!, JsonOptions)!;
        var first = state.CurrentTurnPlayerId!.Value;

        var move = await sut.ApplyMoveAsync(first, sessionId, fromPoint: null, toPoint: 4);

        Assert.True(move.IsSuccess);
        Assert.False(move.State!.GameOver);
        Assert.Equal(GameStatus.InProgress, session.Status);
        Assert.Empty(wallet.Settlements);
        Assert.NotNull(session.GameStateJson);
        Assert.NotNull(move.State.Nzengue);
        Assert.Equal(1, move.State.Nzengue!.Occupancy[4]);
    }

    private GameSession CreateWaitingSession(Guid sessionId) => new()
    {
        Id = sessionId,
        CreatorPlayerId = _creator,
        OpponentPlayerId = _opponent,
        BetAmount = 200m,
        Variant = GameVariant.Nzengue,
        Status = GameStatus.Waiting,
        CreatedAt = DateTime.UtcNow
    };

    private static NzengueGameEngine CreateEngine(
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
            NullLogger<NzengueGameEngine>.Instance);
}
