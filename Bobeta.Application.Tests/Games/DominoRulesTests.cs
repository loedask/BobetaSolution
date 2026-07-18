using Bobeta.Application.Common;
using Bobeta.Application.Games.Domino;
using Xunit;

namespace Bobeta.Application.Tests.Games;

public sealed class DominoRulesTests
{
    private readonly Guid _creator = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _opponent = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _session = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void CreateInitial_DealsSevenEachAndSetsOpeningTile()
    {
        var state = DominoRules.CreateInitial(_session, _creator, _opponent);

        Assert.Equal(7, state.CreatorHand.Count);
        Assert.Equal(7, state.OpponentHand.Count);
        Assert.Equal(14, state.Boneyard.Count);
        Assert.True(state.IsOpening);
        Assert.NotNull(state.OpeningTile);
        Assert.Contains(state.OpeningTile!, HandOfStarter(state));
    }

    [Fact]
    public void TryPlay_OpeningMustUseRequiredTile()
    {
        var state = DominoRules.CreateInitial(_session, _creator, _opponent);
        var starter = state.CurrentTurnPlayerId!.Value;
        var wrong = HandOf(state, starter).First(t => t != state.OpeningTile);
        var (wh, wl) = DominoRules.Parse(wrong);

        var applied = DominoRules.TryApplyAction(
            state, _creator, _opponent, starter, DominoRules.ActionPlay, wh, wl, null, out var error);

        Assert.False(applied);
        Assert.Equal(GameMoveErrorCodes.InvalidMove, error);
    }

    [Fact]
    public void TryPlay_OpeningPlacesTileAndFlipsTurn()
    {
        var state = DominoRules.CreateInitial(_session, _creator, _opponent);
        var starter = state.CurrentTurnPlayerId!.Value;
        var (h, l) = DominoRules.Parse(state.OpeningTile!);

        var applied = DominoRules.TryApplyAction(
            state, _creator, _opponent, starter, DominoRules.ActionPlay, h, l, null, out _);

        Assert.True(applied);
        Assert.False(state.IsOpening);
        Assert.Single(state.Chain);
        Assert.Equal(6, HandOf(state, starter).Count);
        Assert.NotEqual(starter, state.CurrentTurnPlayerId);
    }

    [Fact]
    public void TryPlay_AttachesMatchingEnd()
    {
        var state = new DominoGameState
        {
            CreatorHand = ["6-3", "5-1"],
            OpponentHand = ["4-2"],
            Chain = ["6-6"],
            LeftEnd = 6,
            RightEnd = 6,
            CurrentTurnPlayerId = _creator,
            IsOpening = false
        };

        var applied = DominoRules.TryApplyAction(
            state, _creator, _opponent, _creator, DominoRules.ActionPlay, 6, 3, DominoRules.EndRight, out _);

        Assert.True(applied);
        Assert.Equal(["6-6", "6-3"], state.Chain);
        Assert.Equal(6, state.LeftEnd);
        Assert.Equal(3, state.RightEnd);
        Assert.Equal(_opponent, state.CurrentTurnPlayerId);
    }

    [Fact]
    public void EvaluateAfterAction_EmptyHandWins()
    {
        var state = new DominoGameState
        {
            CreatorHand = [],
            OpponentHand = ["4-2"],
            Chain = ["6-6", "6-3"],
            LeftEnd = 6,
            RightEnd = 3,
            CurrentTurnPlayerId = _opponent,
            IsOpening = false
        };

        var (winner, loser, draw) = DominoRules.EvaluateAfterAction(
            state, _creator, _opponent, _creator, DominoRules.ActionPlay);

        Assert.Equal(_creator, winner);
        Assert.Equal(_opponent, loser);
        Assert.False(draw);
    }

    [Fact]
    public void EvaluateAfterAction_MutualBlockComparesPips()
    {
        var state = new DominoGameState
        {
            CreatorHand = ["1-0"],
            OpponentHand = ["5-5"],
            Boneyard = [],
            Chain = ["6-6"],
            LeftEnd = 6,
            RightEnd = 6,
            CurrentTurnPlayerId = _opponent,
            IsOpening = false
        };

        var (winner, loser, draw) = DominoRules.EvaluateAfterAction(
            state, _creator, _opponent, _creator, DominoRules.ActionPass);

        Assert.Equal(_creator, winner);
        Assert.Equal(_opponent, loser);
        Assert.False(draw);
    }

    [Fact]
    public void EvaluateAfterAction_PassWhenOpponentCanStillPlay_Continues()
    {
        var state = new DominoGameState
        {
            CreatorHand = ["1-0"],
            OpponentHand = ["6-5"],
            Boneyard = [],
            Chain = ["6-6"],
            LeftEnd = 6,
            RightEnd = 6,
            CurrentTurnPlayerId = _opponent,
            IsOpening = false
        };

        var (winner, loser, draw) = DominoRules.EvaluateAfterAction(
            state, _creator, _opponent, _creator, DominoRules.ActionPass);

        Assert.Null(winner);
        Assert.Null(loser);
        Assert.False(draw);
    }

    [Fact]
    public void EvaluateAfterAction_PassWhenBoneyardRemains_Continues()
    {
        var state = new DominoGameState
        {
            CreatorHand = ["1-0"],
            OpponentHand = ["5-5"],
            Boneyard = ["3-2"],
            Chain = ["6-6"],
            LeftEnd = 6,
            RightEnd = 6,
            CurrentTurnPlayerId = _opponent,
            IsOpening = false
        };

        var (winner, loser, draw) = DominoRules.EvaluateAfterAction(
            state, _creator, _opponent, _creator, DominoRules.ActionPass);

        Assert.Null(winner);
        Assert.Null(loser);
        Assert.False(draw);
    }

    [Fact]
    public void EvaluateAfterAction_MutualBlockEqualPips_IsDraw()
    {
        var state = new DominoGameState
        {
            CreatorHand = ["2-0"],
            OpponentHand = ["1-1"],
            Boneyard = [],
            Chain = ["6-6"],
            LeftEnd = 6,
            RightEnd = 6,
            CurrentTurnPlayerId = _opponent,
            IsOpening = false
        };

        var (winner, loser, draw) = DominoRules.EvaluateAfterAction(
            state, _creator, _opponent, _creator, DominoRules.ActionPass);

        Assert.Null(winner);
        Assert.Null(loser);
        Assert.True(draw);
    }

    [Fact]
    public void TryDraw_AddsTileAndKeepsTurn()
    {
        var state = new DominoGameState
        {
            CreatorHand = ["1-0"],
            OpponentHand = ["5-5"],
            Boneyard = ["3-2"],
            Chain = ["6-6"],
            LeftEnd = 6,
            RightEnd = 6,
            CurrentTurnPlayerId = _creator,
            IsOpening = false
        };

        var applied = DominoRules.TryApplyAction(
            state, _creator, _opponent, _creator, DominoRules.ActionDraw, null, null, null, out _);

        Assert.True(applied);
        Assert.Equal(2, state.CreatorHand.Count);
        Assert.Empty(state.Boneyard);
        Assert.Equal(_creator, state.CurrentTurnPlayerId);
    }

    private List<string> HandOfStarter(DominoGameState state) =>
        HandOf(state, state.CurrentTurnPlayerId!.Value);

    private List<string> HandOf(DominoGameState state, Guid playerId) =>
        playerId == _creator ? state.CreatorHand : state.OpponentHand;
}
