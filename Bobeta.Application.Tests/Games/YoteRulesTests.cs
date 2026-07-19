using Bobeta.Application.Common;
using Bobeta.Application.Games.Yote;
using Xunit;

namespace Bobeta.Application.Tests.Games;

public sealed class YoteRulesTests
{
    private readonly Guid _creator = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _opponent = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void CreateInitial_EmptyBoardWithHands()
    {
        var state = YoteRules.CreateInitial(_creator);

        Assert.Equal(_creator, state.CurrentTurnPlayerId);
        Assert.Equal(12, state.CreatorInHand);
        Assert.Equal(12, state.OpponentInHand);
        Assert.All(state.Cells, c => Assert.Null(c));
    }

    [Fact]
    public void TryApply_Place_OccupiesAndPassesTurn()
    {
        var state = YoteRules.CreateInitial(_creator);

        var ok = YoteRules.TryApply(state, _creator, _opponent, _creator, null, 0, null, out _);

        Assert.True(ok);
        Assert.Equal(_creator, state.Cells[0]);
        Assert.Equal(11, state.CreatorInHand);
        Assert.Equal(_opponent, state.CurrentTurnPlayerId);
    }

    [Fact]
    public void TryApply_Slide_MovesOrthogonally()
    {
        var state = YoteRules.CreateInitial(_creator);
        state.CreatorInHand = 11;
        state.Cells[0] = _creator;
        state.CurrentTurnPlayerId = _creator;

        var ok = YoteRules.TryApply(state, _creator, _opponent, _creator, 0, 1, null, out _);

        Assert.True(ok);
        Assert.Null(state.Cells[0]);
        Assert.Equal(_creator, state.Cells[1]);
    }

    [Fact]
    public void TryApply_Capture_RequiresExtraWhenOpponentRemains()
    {
        var state = YoteRules.CreateInitial(_creator);
        state.CreatorInHand = 11;
        state.OpponentInHand = 10;
        // Row0: creator at 0, opponent at 1, empty at 2; another opponent at 7
        state.Cells[0] = _creator;
        state.Cells[1] = _opponent;
        state.Cells[7] = _opponent;
        state.CurrentTurnPlayerId = _creator;

        var missingExtra = YoteRules.TryApply(state, _creator, _opponent, _creator, 0, 2, null, out var err);
        Assert.False(missingExtra);
        Assert.Equal(GameMoveErrorCodes.InvalidMove, err);

        var ok = YoteRules.TryApply(state, _creator, _opponent, _creator, 0, 2, 7, out _);
        Assert.True(ok);
        Assert.Equal(_creator, state.Cells[2]);
        Assert.Null(state.Cells[0]);
        Assert.Null(state.Cells[1]);
        Assert.Null(state.Cells[7]);
    }

    [Fact]
    public void EvaluateAfterMove_AllOpponentGone_Wins()
    {
        var state = YoteRules.CreateInitial(_creator);
        state.CreatorInHand = 0;
        state.OpponentInHand = 0;
        state.Cells[0] = _creator;
        state.CurrentTurnPlayerId = _opponent;

        var (winner, loser, isDraw) = YoteRules.EvaluateAfterMove(state, _creator, _opponent);

        Assert.Equal(_creator, winner);
        Assert.Equal(_opponent, loser);
        Assert.False(isDraw);
    }

    [Fact]
    public void EvaluateAfterMove_BothAtOrBelowThree_Draw()
    {
        var state = YoteRules.CreateInitial(_creator);
        state.CreatorInHand = 1;
        state.OpponentInHand = 1;
        state.Cells[0] = _creator;
        state.Cells[1] = _opponent;
        state.CurrentTurnPlayerId = _creator;

        var (winner, loser, isDraw) = YoteRules.EvaluateAfterMove(state, _creator, _opponent);

        Assert.Null(winner);
        Assert.Null(loser);
        Assert.True(isDraw);
    }
}
