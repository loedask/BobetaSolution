using System.Reflection;
using System.Text.Json;
using Bobeta.Application.Games.Kopo;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bobeta.Application.Tests.Games;

public class KopoGameEngineRulesTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Guid _creator = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _opponent = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task ApplyMoveAsync_WhenOutcomeIsDraw_ReleasesBothBets()
    {
        var stalemate = new KopoGameState
        {
            CurrentTurnPlayerId = _creator,
            NextPieceId = 3,
            Pieces =
            [
                new KopoPiece { Id = 1, OwnerId = _creator, Row = 0, Col = 1 },
                new KopoPiece { Id = 2, OwnerId = _opponent, Row = 9, Col = 0 }
            ]
        };

        var (_, _, isDraw) = KopoRules.CheckOutcome(stalemate, _creator, _opponent);
        Assert.True(isDraw);

        var sessionId = Guid.NewGuid();
        var session = new GameSession
        {
            Id = sessionId,
            CreatorPlayerId = _creator,
            OpponentPlayerId = _opponent,
            BetAmount = 200m,
            Variant = GameVariant.Kopo,
            Status = GameStatus.InProgress,
            CreatedAt = DateTime.UtcNow,
            GameStateJson = JsonSerializer.Serialize(stalemate, JsonOptions)
        };

        var wallet = new RecordingWalletService();
        var sut = CreateEngine(session, wallet);

        await InvokeReleaseBetsAsync(sut, session);

        Assert.Equal(2, wallet.Releases.Count);
        Assert.Contains((_creator, 200m), wallet.Releases);
        Assert.Contains((_opponent, 200m), wallet.Releases);
        Assert.Empty(wallet.Settlements);
    }

    [Fact]
    public async Task ApplyMoveAsync_WhenOpponentEliminated_SettlesWithCommission()
    {
        var sessionId = Guid.NewGuid();
        var session = new GameSession
        {
            Id = sessionId,
            CreatorPlayerId = _creator,
            OpponentPlayerId = _opponent,
            BetAmount = 200m,
            Variant = GameVariant.Kopo,
            Status = GameStatus.InProgress,
            CreatedAt = DateTime.UtcNow,
            GameStateJson = JsonSerializer.Serialize(new KopoGameState
            {
                CurrentTurnPlayerId = _creator,
                NextPieceId = 4,
                Pieces =
                [
                    new KopoPiece { Id = 1, OwnerId = _creator, Row = 4, Col = 3 },
                    new KopoPiece { Id = 2, OwnerId = _opponent, Row = 3, Col = 2 }
                ]
            }, JsonOptions)
        };

        var wallet = new RecordingWalletService();
        var sut = CreateEngine(session, wallet);

        var move = await sut.ApplyMoveAsync(_creator, sessionId, new[] { (4, 3), (2, 1) });

        Assert.True(move.IsSuccess);
        Assert.True(move.State!.GameOver);
        Assert.Equal(_creator, move.State.WinnerPlayerId);
        Assert.Single(wallet.Settlements);
        Assert.Empty(wallet.Releases);
        Assert.NotNull(session.GameResult);
        Assert.Equal(400m, session.GameResult!.TotalPot);
        Assert.Equal(100m, session.GameResult.PlatformCommission);
    }

    private static async Task InvokeReleaseBetsAsync(KopoGameEngine engine, GameSession session)
    {
        var method = typeof(KopoGameEngine).GetMethod("ReleaseBetsAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ReleaseBetsAsync not found.");
        await (Task)method.Invoke(engine, [session, CancellationToken.None])!;
    }

    private static KopoGameEngine CreateEngine(GameSession session, RecordingWalletService wallet) =>
        new(
            new InMemoryGameSessionRepository(session),
            new InMemoryGameMoveRepository(),
            new InMemoryGameResultRepository(result => session.GameResult = result),
            wallet,
            new InMemoryPlayerRepository(
                new Player { Id = session.CreatorPlayerId, PlayerName = "Creator" },
                new Player { Id = session.OpponentPlayerId!.Value, PlayerName = "Opponent" }),
            NoOpGameRevenueService.Instance,
            NullLogger<KopoGameEngine>.Instance);
}
