using Bobeta.Application.Games.Makopa;
using Xunit;

namespace Bobeta.Application.Tests.Games;

/// <summary>Regression tests for Makopa follow-suit and void (Take) rules.</summary>
public class MakopaRulesTests
{
    [Fact]
    public void IsLegalPlay_WhenNoLead_ReturnsTrue()
    {
        var hand = new[] { "Heart_5", "Spade_10" };
        Assert.True(MakopaRules.IsLegalPlay("Heart_5", null, hand));
    }

    [Fact]
    public void IsLegalFollowSuit_WhenVoid_ReturnsFalse()
    {
        var hand = new[] { "Spade_10", "Club_3" };
        Assert.False(MakopaRules.IsLegalFollowSuit("Spade_10", "Heart", hand));
    }

    [Fact]
    public void IsLegalFollowSuit_WhenHasLedSuit_RequiresMatchingSuit()
    {
        var hand = new[] { "Heart_5", "Heart_12", "Spade_2" };
        Assert.True(MakopaRules.IsLegalFollowSuit("Heart_5", "Heart", hand));
        Assert.True(MakopaRules.IsLegalFollowSuit("Heart_12", "Heart", hand));
        Assert.False(MakopaRules.IsLegalFollowSuit("Spade_2", "Heart", hand));
    }

    [Fact]
    public void ResponderNeedsVoidFollow_WhenVoid_ReturnsTrue()
    {
        var hand = new[] { "Spade_10", "Club_3" };
        Assert.True(MakopaRules.ResponderNeedsVoidFollow("Heart_5", hand));
    }

    [Fact]
    public void ResponderNeedsVoidFollow_WhenCanFollow_ReturnsFalse()
    {
        var hand = new[] { "Heart_5", "Spade_10" };
        Assert.False(MakopaRules.ResponderNeedsVoidFollow("Heart_3", hand));
    }

    [Fact]
    public void ResolveTrickWinner_WhenOffSuitResponse_LeaderWinsEvenIfRankIsHigher()
    {
        var leader = Guid.NewGuid();
        var responder = Guid.NewGuid();
        var winner = MakopaRules.ResolveTrickWinner(leader, "Heart_5", responder, "Spade_14", "Heart");
        Assert.Equal(leader, winner);
    }

    [Fact]
    public void ResolveTrickWinner_WhenRanksTieOnLedSuit_LeaderWins()
    {
        var leader = Guid.NewGuid();
        var responder = Guid.NewGuid();
        var winner = MakopaRules.ResolveTrickWinner(leader, "Spade_10", responder, "Spade_10", "Spade");
        Assert.Equal(leader, winner);
    }

    [Fact]
    public void ResolveTrickWinner_WhenHigherRankOnLedSuit_ResponderWins()
    {
        var leader = Guid.NewGuid();
        var responder = Guid.NewGuid();
        var winner = MakopaRules.ResolveTrickWinner(leader, "Spade_5", responder, "Spade_12", "Spade");
        Assert.Equal(responder, winner);
    }
}
