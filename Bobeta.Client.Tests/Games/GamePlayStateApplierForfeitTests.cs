using Bobeta.Client.Models.Api;
using Bobeta.Client.Models.Games;
using Bobeta.Client.Presentation;
using Xunit;

namespace Bobeta.Client.Tests.Games;

/// <summary>
/// Ensures forfeit finishes (game over + winner) are not treated like inactivity cancels (no winner).
/// </summary>
public sealed class GamePlayStateApplierForfeitTests
{
    [Fact]
    public void IsSessionEndedWithoutWinner_WhenCancelledNoWinner_IsTrue()
    {
        var state = new GameStateViewModel
        {
            GameOver = true,
            WinnerPlayerId = null,
            WaitingForGameStart = false,
            IsDraw = false
        };

        Assert.True(GamePlayStateApplier.IsSessionEndedWithoutWinner(state));
    }

    [Fact]
    public void IsSessionEndedWithoutWinner_WhenForfeitHasWinner_IsFalse()
    {
        var winnerId = Guid.NewGuid();
        var state = new GameStateViewModel
        {
            GameOver = true,
            WinnerPlayerId = winnerId,
            WaitingForGameStart = false,
            IsDraw = false
        };

        Assert.False(GamePlayStateApplier.IsSessionEndedWithoutWinner(state));
    }

    [Fact]
    public void ApplyAuthoritativeState_WhenForfeitWin_ShowsGameResultForWinner()
    {
        var myId = Guid.NewGuid();
        var table = new GamePlayTableState();
        var state = new GameStateViewModel
        {
            SessionId = Guid.NewGuid(),
            GameOver = true,
            WinnerPlayerId = myId,
            LobbyPotAmount = 400m,
            Variant = GameVariant.Makopa,
            MyCards = []
        };

        var result = GamePlayStateApplier.ApplyAuthoritativeState(table, state, myId, blockInteraction: false);

        Assert.Equal(GamePlayStateApplier.ApplyResult.GameOver, result);
        Assert.True(table.ShowGameResult);
        Assert.False(table.IsDraw);
        Assert.Equal("You", table.WinnerPlayerName);
    }

    [Fact]
    public void ApplyAuthoritativeState_WhenForfeitLoss_ShowsGameResultForLoser()
    {
        var myId = Guid.NewGuid();
        var winnerId = Guid.NewGuid();
        var table = new GamePlayTableState();
        var state = new GameStateViewModel
        {
            SessionId = Guid.NewGuid(),
            GameOver = true,
            WinnerPlayerId = winnerId,
            LobbyPotAmount = 400m,
            Variant = GameVariant.Makopa,
            MyCards = []
        };

        var result = GamePlayStateApplier.ApplyAuthoritativeState(table, state, myId, blockInteraction: false);

        Assert.Equal(GamePlayStateApplier.ApplyResult.GameOver, result);
        Assert.True(table.ShowGameResult);
        Assert.Equal("Opponent", table.WinnerPlayerName);
    }

    [Fact]
    public void ApplyAuthoritativeState_WhenInactivityCancel_ReturnsSessionEnded()
    {
        var table = new GamePlayTableState { ShowGameResult = false };
        var state = new GameStateViewModel
        {
            SessionId = Guid.NewGuid(),
            GameOver = true,
            WinnerPlayerId = null,
            WaitingForGameStart = false,
            IsDraw = false,
            Variant = GameVariant.Makopa,
            MyCards = []
        };

        var result = GamePlayStateApplier.ApplyAuthoritativeState(table, state, Guid.NewGuid(), blockInteraction: false);

        Assert.Equal(GamePlayStateApplier.ApplyResult.SessionEnded, result);
        Assert.False(table.ShowGameResult);
    }
}
