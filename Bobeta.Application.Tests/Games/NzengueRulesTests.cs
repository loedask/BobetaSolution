using Bobeta.Application.Common;
using Bobeta.Application.Games.Nzengue;
using Xunit;

namespace Bobeta.Application.Tests.Games;

public sealed class NzengueRulesTests
{
    private readonly Guid _creator = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _opponent = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void CreateInitial_StartsInPlacePhaseWithThreeEach()
    {
        var state = NzengueRules.CreateInitial(_creator);

        Assert.Equal(_creator, state.CurrentTurnPlayerId);
        Assert.Equal(3, state.CreatorPiecesToPlace);
        Assert.Equal(3, state.OpponentPiecesToPlace);
        Assert.Equal(NzengueRules.PhasePlace, NzengueRules.PhaseOf(state));
    }

    [Fact]
    public void TryApplyPlace_RejectsWrongTurn()
    {
        var state = NzengueRules.CreateInitial(_creator);

        var applied = NzengueRules.TryApplyPlace(state, _creator, _opponent, _opponent, 0, out var error);

        Assert.False(applied);
        Assert.Equal(GameMoveErrorCodes.NotYourTurn, error);
    }

    [Fact]
    public void TryApplyPlace_OccupiesPointAndPassesTurn()
    {
        var state = NzengueRules.CreateInitial(_creator);

        var applied = NzengueRules.TryApplyPlace(state, _creator, _opponent, _creator, 4, out _);

        Assert.True(applied);
        Assert.Equal(_creator, state.Points[4]);
        Assert.Equal(2, state.CreatorPiecesToPlace);
        Assert.Equal(_opponent, state.CurrentTurnPlayerId);
    }

    [Fact]
    public void TryApplyPlace_WinsOnThreeInALine()
    {
        var state = NzengueRules.CreateInitial(_creator);
        state.Points[0] = _creator;
        state.Points[1] = _creator;
        state.CreatorPiecesToPlace = 1;
        state.OpponentPiecesToPlace = 0;

        var applied = NzengueRules.TryApplyPlace(state, _creator, _opponent, _creator, 2, out _);

        Assert.True(applied);
        Assert.True(NzengueRules.HasLine(state, _creator));
        var (winner, loser, isDraw) = NzengueRules.EvaluateAfterMove(state, _creator, _opponent, _creator);
        Assert.Equal(_creator, winner);
        Assert.Equal(_opponent, loser);
        Assert.False(isDraw);
    }

    [Fact]
    public void TryApplyMove_RequiresAdjacentEmptyPoint()
    {
        var state = new NzengueGameState
        {
            CreatorPiecesToPlace = 0,
            OpponentPiecesToPlace = 0,
            CurrentTurnPlayerId = _creator,
            Points = [ _creator, null, _opponent, null, null, null, null, null, null ]
        };

        var far = NzengueRules.TryApplyMove(state, _creator, _opponent, _creator, 0, 2, out var error);
        Assert.False(far);
        Assert.Equal(GameMoveErrorCodes.InvalidMove, error);

        var ok = NzengueRules.TryApplyMove(state, _creator, _opponent, _creator, 0, 1, out _);
        Assert.True(ok);
        Assert.Null(state.Points[0]);
        Assert.Equal(_creator, state.Points[1]);
    }

    [Fact]
    public void AreAdjacent_CenterConnectsToAllEdges()
    {
        for (var i = 0; i < 9; i++)
        {
            if (i == 4)
                continue;
            Assert.True(NzengueRules.AreAdjacent(4, i));
        }
    }
}
