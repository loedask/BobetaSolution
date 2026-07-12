using System.Text.Json;
using Bobeta.Application.Common;
using Bobeta.Application.Games.Makopa;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Bobeta.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bobeta.Application.Tests.Games;

/// <summary>Regression tests for Makopa trick resolution, win/loss, and setup rules.</summary>
public class MakopaGameEngineRulesTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public async Task StartGame_DealsFourCardsEach_From52CardDeck()
    {
        var sessionId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var session = new GameSession
        {
            Id = sessionId,
            CreatorPlayerId = creatorId,
            OpponentPlayerId = opponentId,
            BetAmount = 200m,
            Status = GameStatus.Waiting,
            CreatedAt = DateTime.UtcNow
        };

        var sut = CreateEngine(session);
        await sut.StartGameAsync(session);

        Assert.Equal(GameStatus.InProgress, session.Status);
        Assert.NotNull(session.GameStateJson);
        var state = JsonSerializer.Deserialize<MakopaGameState>(session.GameStateJson!, JsonOptions)!;
        Assert.Equal(4, state.CreatorHand.Count);
        Assert.Equal(4, state.OpponentHand.Count);
        Assert.Equal(8, state.CreatorHand.Count + state.OpponentHand.Count);
        Assert.Empty(state.TrickPlays);
        Assert.NotNull(state.CurrentTurnPlayerId);
        Assert.Equal(state.LeadPlayerId, state.CurrentTurnPlayerId);
    }

    [Fact]
    public async Task PlayCard_WhenHigherRankOnLedSuit_WinnerLeadsNextTrick()
    {
        var sessionId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var pendingState = new MakopaGameState
        {
            CreatorHand = new List<string> { "Club_9", "Diamond_2" },
            OpponentHand = new List<string> { "Spade_12", "Heart_10", "Club_3" },
            LeadPlayerId = creatorId,
            CurrentTurnPlayerId = opponentId,
            TrickSuit = "Spade",
            TrickPlays = new List<PlayedInTrick>
            {
                new() { PlayerId = creatorId, Card = "Spade_5" }
            }
        };

        var session = CreateSession(sessionId, creatorId, opponentId, pendingState);
        var sut = CreateEngine(session);

        var move = await sut.PlayCardAsync(opponentId, sessionId, new Card(CardSuit.Spade, CardRank.Queen));

        Assert.True(move.IsSuccess);
        Assert.False(move.State!.GameOver);
        var state = JsonSerializer.Deserialize<MakopaGameState>(session.GameStateJson!, JsonOptions)!;
        Assert.Empty(state.TrickPlays);
        Assert.Equal(opponentId, state.LeadPlayerId);
        Assert.Equal(opponentId, state.CurrentTurnPlayerId);
        Assert.Equal(opponentId, state.LastTrickWinnerPlayerId);
    }

    [Fact]
    public async Task PlayCard_WhenVoidButPlaysCard_ReturnsMustFollowSuit()
    {
        var sessionId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var pendingState = new MakopaGameState
        {
            CreatorHand = new List<string> { "Heart_5" },
            OpponentHand = new List<string> { "Spade_10", "Club_3" },
            LeadPlayerId = creatorId,
            CurrentTurnPlayerId = opponentId,
            TrickSuit = "Heart",
            TrickPlays = new List<PlayedInTrick>
            {
                new() { PlayerId = creatorId, Card = "Heart_5" }
            }
        };

        var session = CreateSession(sessionId, creatorId, opponentId, pendingState);
        var sut = CreateEngine(session);

        var move = await sut.PlayCardAsync(opponentId, sessionId, new Card(CardSuit.Spade, CardRank.Ten));

        Assert.False(move.IsSuccess);
        Assert.Equal(GameMoveErrorCodes.MustFollowSuit, move.ErrorCode);
    }

    [Fact]
    public async Task PlayCard_WhenHasLedSuitButPlaysOffSuit_ReturnsMustFollowSuit()
    {
        var sessionId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var pendingState = new MakopaGameState
        {
            CreatorHand = new List<string> { "Heart_5" },
            OpponentHand = new List<string> { "Heart_10", "Spade_3" },
            LeadPlayerId = creatorId,
            CurrentTurnPlayerId = opponentId,
            TrickSuit = "Heart",
            TrickPlays = new List<PlayedInTrick>
            {
                new() { PlayerId = creatorId, Card = "Heart_5" }
            }
        };

        var session = CreateSession(sessionId, creatorId, opponentId, pendingState);
        var sut = CreateEngine(session);

        var move = await sut.PlayCardAsync(opponentId, sessionId, new Card(CardSuit.Spade, CardRank.Three));

        Assert.False(move.IsSuccess);
        Assert.Equal(GameMoveErrorCodes.MustFollowSuit, move.ErrorCode);
    }

    [Fact]
    public async Task PlayCard_InstantLoss_WhenResponderMatchesSingletonHolderSuit()
    {
        var sessionId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var pendingState = new MakopaGameState
        {
            CreatorHand = new List<string> { "Spade_2" },
            OpponentHand = new List<string> { "Spade_10", "Heart_3" },
            LeadPlayerId = creatorId,
            CurrentTurnPlayerId = opponentId,
            TrickSuit = "Spade",
            TrickPlays = new List<PlayedInTrick>
            {
                new() { PlayerId = creatorId, Card = "Spade_5" }
            }
        };

        var session = CreateSession(sessionId, creatorId, opponentId, pendingState);
        var sut = CreateEngine(session);

        var move = await sut.PlayCardAsync(opponentId, sessionId, new Card(CardSuit.Spade, CardRank.Ten));

        Assert.True(move.IsSuccess);
        Assert.True(move.State!.GameOver);
        Assert.Equal(creatorId, move.State.WinnerPlayerId);
        Assert.Equal(GameStatus.Finished, session.Status);
    }

    [Fact]
    public async Task VoidFollow_AddsLeadCardToResponder_AndLeaderLeadsAgain()
    {
        var sessionId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var pendingState = new MakopaGameState
        {
            CreatorHand = new List<string> { "Heart_5", "Club_2" },
            OpponentHand = new List<string> { "Spade_10", "Diamond_3" },
            LeadPlayerId = creatorId,
            CurrentTurnPlayerId = opponentId,
            TrickSuit = "Heart",
            TrickPlays = new List<PlayedInTrick>
            {
                new() { PlayerId = creatorId, Card = "Heart_5" }
            }
        };

        var session = CreateSession(sessionId, creatorId, opponentId, pendingState);
        var sut = CreateEngine(session);

        var move = await sut.VoidFollowDrawAsync(opponentId, sessionId);

        Assert.True(move.IsSuccess);
        Assert.False(move.State!.GameOver);
        var state = JsonSerializer.Deserialize<MakopaGameState>(session.GameStateJson!, JsonOptions)!;
        Assert.Contains("Heart_5", state.OpponentHand);
        Assert.Empty(state.TrickPlays);
        Assert.Null(state.TrickSuit);
        Assert.Equal(creatorId, state.LeadPlayerId);
        Assert.Equal(creatorId, state.CurrentTurnPlayerId);
    }

    [Fact]
    public async Task PlayCard_WhenTrickCompleted_IncrementsWinnerRoundWins()
    {
        var sessionId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var pendingState = new MakopaGameState
        {
            CreatorHand = new List<string> { "Club_9", "Diamond_2" },
            OpponentHand = new List<string> { "Spade_12", "Heart_10", "Club_3" },
            LeadPlayerId = creatorId,
            CurrentTurnPlayerId = opponentId,
            TrickSuit = "Spade",
            TrickPlays = new List<PlayedInTrick>
            {
                new() { PlayerId = creatorId, Card = "Spade_5" }
            }
        };

        var session = CreateSession(sessionId, creatorId, opponentId, pendingState);
        var sut = CreateEngine(session);

        var move = await sut.PlayCardAsync(opponentId, sessionId, new Card(CardSuit.Spade, CardRank.Queen));

        Assert.True(move.IsSuccess);
        Assert.Equal(1, move.State!.MyRoundWins);
        Assert.Equal(0, move.State.OpponentRoundWins);

        var creatorView = await sut.GetGameStateAsync(session, creatorId);
        Assert.NotNull(creatorView);
        Assert.Equal(0, creatorView!.MyRoundWins);
        Assert.Equal(1, creatorView.OpponentRoundWins);
    }

    [Fact]
    public async Task WinOnLead_Applies25PercentPlatformCommission()
    {
        var sessionId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        const decimal betAmount = 200m;
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

        var session = CreateSession(sessionId, creatorId, opponentId, pendingState, betAmount);
        var resultRepo = new InMemoryGameResultRepository(result => session.GameResult = result);
        var sut = CreateEngine(session, resultRepo);

        await sut.PlayCardAsync(opponentId, sessionId, new Card(CardSuit.Spade, CardRank.Queen));

        Assert.NotNull(session.GameResult);
        Assert.Equal(betAmount * 2, session.GameResult!.TotalPot);
        Assert.Equal(betAmount * 2 * 0.25m, session.GameResult.PlatformCommission);
        Assert.Equal(betAmount * 2 * 0.75m, session.GameResult.WinnerAmount);
    }

    private static GameSession CreateSession(
        Guid sessionId,
        Guid creatorId,
        Guid opponentId,
        MakopaGameState state,
        decimal betAmount = 200m) => new()
    {
        Id = sessionId,
        CreatorPlayerId = creatorId,
        OpponentPlayerId = opponentId,
        BetAmount = betAmount,
        Status = GameStatus.InProgress,
        CreatedAt = DateTime.UtcNow,
        GameStateJson = JsonSerializer.Serialize(state, JsonOptions)
    };

    private static MakopaGameEngine CreateEngine(
        GameSession session,
        InMemoryGameResultRepository? resultRepo = null)
    {
        resultRepo ??= new InMemoryGameResultRepository(result => session.GameResult = result);
        return new MakopaGameEngine(
            new InMemoryGameSessionRepository(session),
            new InMemoryGameMoveRepository(),
            resultRepo,
            new RecordingWalletService(),
            new InMemoryPlayerRepository(
                new Player { Id = session.CreatorPlayerId, PlayerName = "Creator" },
                new Player { Id = session.OpponentPlayerId!.Value, PlayerName = "Opponent" }),
            NoOpGameRevenueService.Instance,
            NullLogger<MakopaGameEngine>.Instance);
    }
}
