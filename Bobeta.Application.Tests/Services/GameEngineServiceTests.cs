using System.Text.Json;
using Bobeta.Application.Games.Makopa;
using Bobeta.Application.Tests.Games;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Bobeta.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bobeta.Application.Tests.Services;

public class GameEngineServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public async Task PlayCardAsync_WhenTrickWinnerHasOneCardToLead_EndsGameInSameCall()
    {
        var sessionId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var pendingState = new MakopaGameState
        {
            CreatorHand = new List<string>(),
            OpponentHand = new List<string> { "Spade_12", "Heart_10" },
            LeadPlayerId = creatorId,
            CurrentTurnPlayerId = opponentId,
            TrickSuit = "Spade",
            TrickPlays = new List<PlayedInTrick>
            {
                new() { PlayerId = creatorId, Card = "Spade_5" }
            }
        };

        var session = CreateSession(sessionId, creatorId, opponentId, pendingState);
        var wallet = new RecordingWalletService();
        var sut = CreateEngine(session, wallet);

        var move = await sut.PlayCardAsync(opponentId, sessionId, new Card(CardSuit.Spade, CardRank.Queen));

        Assert.True(move.IsSuccess);
        Assert.NotNull(move.State);
        Assert.True(move.State!.GameOver);
        Assert.Equal(opponentId, move.State.WinnerPlayerId);
        Assert.Equal(GameStatus.Finished, session.Status);
        Assert.Null(session.GameStateJson);
        Assert.NotNull(session.GameResult);
        Assert.Equal(opponentId, session.GameResult!.WinnerPlayerId);
        Assert.Equal(creatorId, session.GameResult.LoserPlayerId);
        Assert.Single(wallet.Settlements);
    }

    [Fact]
    public async Task VoidFollowDrawAsync_WhenLeaderHasOneCardToLead_EndsGameInSameCall()
    {
        var sessionId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var pendingState = new MakopaGameState
        {
            CreatorHand = new List<string> { "Spade_2" },
            OpponentHand = new List<string> { "Club_10", "Diamond_3" },
            LeadPlayerId = creatorId,
            CurrentTurnPlayerId = opponentId,
            TrickSuit = "Heart",
            TrickPlays = new List<PlayedInTrick>
            {
                new() { PlayerId = creatorId, Card = "Heart_5" }
            }
        };

        var session = CreateSession(sessionId, creatorId, opponentId, pendingState);
        var wallet = new RecordingWalletService();
        var sut = CreateEngine(session, wallet);

        var move = await sut.VoidFollowDrawAsync(opponentId, sessionId);

        Assert.True(move.IsSuccess);
        Assert.NotNull(move.State);
        Assert.True(move.State!.GameOver);
        Assert.Equal(creatorId, move.State.WinnerPlayerId);
        Assert.Equal(GameStatus.Finished, session.Status);
        Assert.Single(wallet.Settlements);
    }

    [Fact]
    public async Task PlayCardAsync_WhenTrickWinnerStillHasCards_GameContinues()
    {
        var sessionId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var pendingState = new MakopaGameState
        {
            CreatorHand = new List<string> { "Club_9" },
            OpponentHand = new List<string> { "Heart_10", "Club_2", "Spade_3" },
            LeadPlayerId = creatorId,
            CurrentTurnPlayerId = opponentId,
            TrickSuit = "Heart",
            TrickPlays = new List<PlayedInTrick>
            {
                new() { PlayerId = creatorId, Card = "Heart_5" }
            }
        };

        var session = CreateSession(sessionId, creatorId, opponentId, pendingState);
        var wallet = new RecordingWalletService();
        var sut = CreateEngine(session, wallet);

        var move = await sut.PlayCardAsync(opponentId, sessionId, new Card(CardSuit.Heart, CardRank.Ten));

        Assert.True(move.IsSuccess);
        Assert.NotNull(move.State);
        Assert.False(move.State!.GameOver);
        Assert.Null(move.State.WinnerPlayerId);
        Assert.Equal(GameStatus.InProgress, session.Status);
        Assert.Empty(wallet.Settlements);
    }

    private static GameSession CreateSession(Guid sessionId, Guid creatorId, Guid opponentId, MakopaGameState state) => new()
    {
        Id = sessionId,
        CreatorPlayerId = creatorId,
        OpponentPlayerId = opponentId,
        BetAmount = 100m,
        Status = GameStatus.InProgress,
        CreatedAt = DateTime.UtcNow,
        GameStateJson = JsonSerializer.Serialize(state, JsonOptions)
    };

    private static MakopaGameEngine CreateEngine(GameSession session, RecordingWalletService wallet)
    {
        return new MakopaGameEngine(
            new InMemoryGameSessionRepository(session),
            new InMemoryGameMoveRepository(),
            new InMemoryGameResultRepository(result => session.GameResult = result),
            wallet,
            new InMemoryPlayerRepository(
                new Player { Id = session.CreatorPlayerId, PlayerName = "Creator" },
                new Player { Id = session.OpponentPlayerId!.Value, PlayerName = "Opponent" }),
            NoOpGameRevenueService.Instance,
            NoOpNotificationService.Instance,
            NullLogger<MakopaGameEngine>.Instance);
    }
}
