using System.Text.Json;
using Bobeta.Application.Games.Yote;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bobeta.Application.Tests.Games;

public class YoteGameEngineRulesTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Guid _creator = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _opponent = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task ApplyMoveAsync_Place_ContinuesWithoutSettlement()
    {
        var sessionId = Guid.NewGuid();
        var session = CreateWaitingSession(sessionId);
        var wallet = new RecordingWalletService();
        var sut = CreateEngine(session, wallet);

        await sut.StartGameAsync(session);
        var state = JsonSerializer.Deserialize<YoteGameState>(session.GameStateJson!, JsonOptions)!;
        var first = state.CurrentTurnPlayerId!.Value;

        var move = await sut.ApplyMoveAsync(first, sessionId, null, 3, null);

        Assert.True(move.IsSuccess);
        Assert.False(move.State!.GameOver);
        Assert.Empty(wallet.Settlements);
        Assert.NotNull(session.GameStateJson);
        Assert.NotNull(move.State.Yote);
        Assert.Equal(1, move.State.Yote!.Occupancy[3]);
    }

    [Fact]
    public async Task ApplyMoveAsync_CaptureClearsOpponent_Settles()
    {
        var sessionId = Guid.NewGuid();
        var session = CreateWaitingSession(sessionId);
        var wallet = new RecordingWalletService();
        var notifications = new RecordingNotificationService();
        var sut = CreateEngine(session, wallet, notifications);

        await sut.StartGameAsync(session);
        var state = JsonSerializer.Deserialize<YoteGameState>(session.GameStateJson!, JsonOptions)!;
        state.CreatorInHand = 0;
        state.OpponentInHand = 0;
        state.Cells[0] = _creator;
        state.Cells[1] = _opponent;
        state.CurrentTurnPlayerId = _creator;
        session.GameStateJson = JsonSerializer.Serialize(state, JsonOptions);

        var move = await sut.ApplyMoveAsync(_creator, sessionId, 0, 2, null);

        Assert.True(move.IsSuccess);
        Assert.True(move.State!.GameOver);
        Assert.Equal(_creator, move.State.WinnerPlayerId);
        Assert.Single(wallet.Settlements);
        Assert.Equal(2, notifications.GameResults.Count);
        Assert.Null(session.GameStateJson);
    }

    private GameSession CreateWaitingSession(Guid sessionId) => new()
    {
        Id = sessionId,
        CreatorPlayerId = _creator,
        OpponentPlayerId = _opponent,
        BetAmount = 200m,
        Variant = GameVariant.Yote,
        Status = GameStatus.Waiting,
        CreatedAt = DateTime.UtcNow
    };

    private static YoteGameEngine CreateEngine(
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
            NullLogger<YoteGameEngine>.Instance);
}
