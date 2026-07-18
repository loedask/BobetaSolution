using Bobeta.Application.Common;
using Bobeta.Application.Games.Abbia;
using Xunit;

namespace Bobeta.Application.Tests.Games;

public sealed class AbbiaRulesTests
{
    private readonly Guid _creator = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _opponent = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _session = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void CreateInitial_SetsFirstTurn()
    {
        var state = AbbiaRules.CreateInitial(_session, _creator, _opponent);

        Assert.NotNull(state.CurrentTurnPlayerId);
        Assert.True(state.CurrentTurnPlayerId == _creator || state.CurrentTurnPlayerId == _opponent);
        Assert.False(state.CreatorThrown);
        Assert.False(state.OpponentThrown);
    }

    [Fact]
    public void TryApplyThrow_RejectsWrongTurn()
    {
        var state = AbbiaRules.CreateInitial(_session, _creator, _opponent);
        var current = state.CurrentTurnPlayerId!.Value;
        var other = current == _creator ? _opponent : _creator;

        var applied = AbbiaRules.TryApplyThrow(state, _creator, _opponent, other, _session, 0, out var error);

        Assert.False(applied);
        Assert.Equal(GameMoveErrorCodes.NotYourTurn, error);
    }

    [Fact]
    public void TryApplyThrow_FlipsFiveTokensAndPassesTurn()
    {
        var state = AbbiaRules.CreateInitial(_session, _creator, _opponent);
        var first = state.CurrentTurnPlayerId!.Value;

        var applied = AbbiaRules.TryApplyThrow(state, _creator, _opponent, first, _session, 0, out _);

        Assert.True(applied);
        var tokens = first == _creator ? state.CreatorTokens : state.OpponentTokens;
        Assert.Equal(AbbiaRules.TokenCount, tokens.Count);
        Assert.NotEqual(first, state.CurrentTurnPlayerId);
    }

    [Fact]
    public void Evaluate_HigherCarvedUpWins()
    {
        var state = new AbbiaGameState
        {
            CreatorThrown = true,
            OpponentThrown = true,
            CreatorTokens = [true, true, true, false, false],
            OpponentTokens = [true, false, false, false, false]
        };

        var (winner, loser, isDraw) = AbbiaRules.Evaluate(state, _creator, _opponent);

        Assert.Equal(_creator, winner);
        Assert.Equal(_opponent, loser);
        Assert.False(isDraw);
    }

    [Fact]
    public void Evaluate_EqualCountsAreDraw()
    {
        var state = new AbbiaGameState
        {
            CreatorThrown = true,
            OpponentThrown = true,
            CreatorTokens = [true, true, false, false, false],
            OpponentTokens = [true, false, true, false, false]
        };

        var (winner, loser, isDraw) = AbbiaRules.Evaluate(state, _creator, _opponent);

        Assert.Null(winner);
        Assert.Null(loser);
        Assert.True(isDraw);
    }

    [Fact]
    public void FlipTokens_IsDeterministicForSeed()
    {
        var a = AbbiaRules.FlipTokens(_session, _creator, 1);
        var b = AbbiaRules.FlipTokens(_session, _creator, 1);
        Assert.Equal(a, b);
    }
}
