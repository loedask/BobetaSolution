namespace Bobeta.Application.Games.Makopa;

/// <summary>Makopa trick-taking: follow led suit when possible; void requires Take (no card play).</summary>
public static class MakopaRules
{
    public static bool IsLegalPlay(string cardToPlay, string? leadCardPlayedInTrick, IReadOnlyList<string> myHandCardStrings)
    {
        if (string.IsNullOrEmpty(leadCardPlayedInTrick))
            return true;
        var sep = leadCardPlayedInTrick.IndexOf('_', StringComparison.Ordinal);
        if (sep <= 0)
            return true;
        var leadSuit = leadCardPlayedInTrick[..sep];
        if (!HandContainsLedSuit(leadSuit, myHandCardStrings))
            return false;
        return cardToPlay.StartsWith(leadSuit, StringComparison.Ordinal);
    }

    public static bool IsLegalFollowSuit(string cardToPlay, string? trickLeadSuit, IReadOnlyList<string> myHandCardStrings)
    {
        if (string.IsNullOrEmpty(trickLeadSuit))
            return true;
        if (!HandContainsLedSuit(trickLeadSuit, myHandCardStrings))
            return false;
        return cardToPlay.StartsWith(trickLeadSuit, StringComparison.Ordinal);
    }

    public static bool ResponderNeedsVoidFollow(string? lastPlayedLeadCardDisplay, IReadOnlyList<string> myHandCardStrings)
    {
        if (string.IsNullOrEmpty(lastPlayedLeadCardDisplay))
            return false;
        var sep = lastPlayedLeadCardDisplay.IndexOf('_', StringComparison.Ordinal);
        if (sep <= 0)
            return false;
        var leadSuit = lastPlayedLeadCardDisplay[..sep];
        return !HandContainsLedSuit(leadSuit, myHandCardStrings);
    }

    public static bool HandContainsLedSuit(string trickLeadSuit, IReadOnlyList<string> myHandCardStrings) =>
        myHandCardStrings.Any(c => c.StartsWith(trickLeadSuit, StringComparison.Ordinal));

    /// <summary>Winning seat: higher rank on <paramref name="leadSuit"/>; equal ranks → leader (<paramref name="firstPlayerId"/>).</summary>
    public static Guid ResolveTrickWinner(Guid firstPlayerId, string firstCard, Guid secondPlayerId, string secondCard, string leadSuit)
    {
        var r1 = RankOnLeadCard(firstCard, leadSuit);
        var r2 = RankOnLeadCard(secondCard, leadSuit);
        return r1 >= r2 ? firstPlayerId : secondPlayerId;
    }

    private static int RankOnLeadCard(string card, string leadSuit)
    {
        var parts = card.Split('_', 2, StringSplitOptions.None);
        if (parts.Length < 2)
            return 0;
        if (!string.Equals(parts[0], leadSuit, StringComparison.Ordinal))
            return 0;
        return int.TryParse(parts[1], out var n) ? n : 0;
    }
}
