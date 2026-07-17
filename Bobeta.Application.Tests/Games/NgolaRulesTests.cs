using Bobeta.Application.Common;
using Bobeta.Application.Games.Ngola;
using Xunit;

namespace Bobeta.Application.Tests.Games;

public sealed class NgolaRulesTests
{
    private readonly Guid _creator = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _opponent = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void CreateInitial_PlacesFourSeedsInEachOfSixteenPits()
    {
        var state = NgolaRules.CreateInitial(_creator);

        Assert.Equal(64, state.Pits.Sum());
        Assert.All(state.Pits, seeds => Assert.Equal(4, seeds));
        Assert.Equal(_creator, state.CurrentTurnPlayerId);
    }

    [Fact]
    public void TryApplyMove_SowsCounterclockwiseAndChangesTurn()
    {
        var state = NgolaRules.CreateInitial(_creator);

        var applied = NgolaRules.TryApplyMove(
            state, _creator, _opponent, _creator, 0, out var error);

        Assert.True(applied);
        Assert.Null(error);
        Assert.Equal(0, state.Pits[0]);
        Assert.Equal(5, state.Pits[1]);
        Assert.Equal(5, state.Pits[4]);
        Assert.Equal(_opponent, state.CurrentTurnPlayerId);
    }

    [Fact]
    public void TryApplyMove_RejectsPitWithFewerThanTwoSeeds()
    {
        var state = NgolaRules.CreateInitial(_creator);
        state.Pits[0] = 1;

        var applied = NgolaRules.TryApplyMove(
            state, _creator, _opponent, _creator, 0, out var error);

        Assert.False(applied);
        Assert.Equal(GameMoveErrorCodes.InvalidMove, error);
    }

    [Fact]
    public void TryApplyMove_CapturesOccupiedOpponentLandingPit()
    {
        var state = new NgolaGameState
        {
            CurrentTurnPlayerId = _creator,
            Pits = new int[NgolaRules.TotalPits]
        };
        state.Pits[6] = 2;
        state.Pits[8] = 3;

        var applied = NgolaRules.TryApplyMove(
            state, _creator, _opponent, _creator, 6, out _);

        Assert.True(applied);
        Assert.Equal(0, state.Pits[8]);
        Assert.Equal(4, state.CreatorScore);
    }

    [Fact]
    public void CompleteIfBlocked_CollectsRemainingSeedsAndChoosesWinner()
    {
        var state = new NgolaGameState
        {
            CurrentTurnPlayerId = _opponent,
            CreatorScore = 20,
            OpponentScore = 10,
            Pits = new[] { 1, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 }
        };

        var outcome = NgolaRules.CompleteIfBlocked(state, _creator, _opponent);

        Assert.Equal(_creator, outcome.WinnerId);
        Assert.Equal(_opponent, outcome.LoserId);
        Assert.False(outcome.IsDraw);
        Assert.Equal(22, state.CreatorScore);
        Assert.Equal(11, state.OpponentScore);
        Assert.All(state.Pits, seeds => Assert.Equal(0, seeds));
    }
}
