using Bobeta.Client.Presentation;
using Xunit;

namespace Bobeta.Client.Tests.Games;

public sealed class GamePlayLeavePolicyTests
{
    [Fact]
    public void ShouldConfirmLeave_WhenInProgressMatch_IsTrue()
    {
        Assert.True(GamePlayLeavePolicy.ShouldConfirmLeave(
            allowLeaveWithoutForfeit: false,
            leaveInProgress: false,
            showGameResult: false,
            waitingForOpponent: false,
            sessionId: Guid.NewGuid().ToString("D"),
            hasPlayerId: true));
    }

    [Theory]
    [InlineData(true, false, false, false, true, true)]  // already allowed to leave
    [InlineData(false, true, false, false, true, true)]  // leave in progress
    [InlineData(false, false, true, false, true, true)]  // result modal showing
    [InlineData(false, false, false, true, true, true)]  // waiting for opponent
    [InlineData(false, false, false, false, false, true)] // no session
    [InlineData(false, false, false, false, true, false)] // no player
    public void ShouldConfirmLeave_WhenNotActiveMatch_IsFalse(
        bool allowLeaveWithoutForfeit,
        bool leaveInProgress,
        bool showGameResult,
        bool waitingForOpponent,
        bool hasSession,
        bool hasPlayerId)
    {
        Assert.False(GamePlayLeavePolicy.ShouldConfirmLeave(
            allowLeaveWithoutForfeit,
            leaveInProgress,
            showGameResult,
            waitingForOpponent,
            hasSession ? Guid.NewGuid().ToString("D") : "",
            hasPlayerId));
    }
}
