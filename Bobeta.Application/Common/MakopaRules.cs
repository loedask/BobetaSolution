namespace Bobeta.Application.Common;

/// <summary>Makopa trick-taking rules shared by server engine and helpers (e.g. test AI).</summary>
public static class MakopaRules
{
    /// <summary>Follow-suit constraint: must play led suit when the hand contains at least one such card.</summary>
    public static bool IsLegalPlay(string cardToPlay, string? leadCardPlayedInTrick, IReadOnlyList<string> myHandCardStrings)
    {
        if (string.IsNullOrEmpty(leadCardPlayedInTrick))
            return true;
        var sep = leadCardPlayedInTrick.IndexOf('_', StringComparison.Ordinal);
        if (sep <= 0)
            return true;
        var leadSuit = leadCardPlayedInTrick[..sep];
        var hasLeadSuit = myHandCardStrings.Any(c => c.StartsWith(leadSuit, StringComparison.Ordinal));
        if (!hasLeadSuit)
            return true;
        return cardToPlay.StartsWith(leadSuit, StringComparison.Ordinal);
    }

    /// <summary>Equivalent check when trick suit is known (avoid parsing first-card string twice).</summary>
    /// <remarks>When the responder has no led suit, they must take the void-follow path rather than playing a card.</remarks>
    public static bool IsLegalFollowSuit(string cardToPlay, string? trickLeadSuit, IReadOnlyList<string> myHandCardStrings)
    {
        if (string.IsNullOrEmpty(trickLeadSuit))
            return true;
        if (!HandContainsLedSuit(trickLeadSuit, myHandCardStrings))
            return false;
        return cardToPlay.StartsWith(trickLeadSuit, StringComparison.Ordinal);
    }

    /// <summary>True if trick is waiting on a follower and follower has zero cards of the led suit → must use void-follow (Take).</summary>
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
}
