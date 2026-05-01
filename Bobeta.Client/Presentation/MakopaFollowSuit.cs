namespace Bobeta.Client.Presentation;

/// <summary>Mirrors server: follow suit if you hold that suit; if you cannot follow, tap Draw instead of playing a card.</summary>
public static class MakopaFollowSuit
{
    /// <summary>You have no cards of the led suit — UI should offer void-follow instead of enabling any play.</summary>
    public static bool ResponderNeedsVoidFollow(string? lastPlayedCard, IReadOnlyList<string> myHandDisplayValues)
    {
        if (string.IsNullOrEmpty(lastPlayedCard))
            return false;
        var leadSuit = SuitPrefix(lastPlayedCard);
        if (string.IsNullOrEmpty(leadSuit))
            return false;
        return !myHandDisplayValues.Any(c => c.StartsWith(leadSuit, StringComparison.Ordinal));
    }

    /// <summary>True when this card may be submitted while responding to a lead (same rule as server).</summary>
    public static bool IsLegalPlay(string cardToPlay, string? lastPlayedCard, IReadOnlyList<string> myHandDisplayValues)
    {
        if (ResponderNeedsVoidFollow(lastPlayedCard, myHandDisplayValues))
            return false;
        if (string.IsNullOrEmpty(lastPlayedCard))
            return true;
        var leadSuit = SuitPrefix(lastPlayedCard);
        if (string.IsNullOrEmpty(leadSuit))
            return true;
        return cardToPlay.StartsWith(leadSuit, StringComparison.Ordinal);
    }

    /// <summary>Whether follow-suit restrictions apply (it is our turn in an active trick).</summary>
    public static bool RulesApply(bool isPlayerTurn, bool waitingForOpponent, bool showGameResult) =>
        isPlayerTurn && !waitingForOpponent && !showGameResult;

    private static string? SuitPrefix(string card)
    {
        var sep = card.IndexOf('_');
        if (sep <= 0)
            return null;
        return card[..sep];
    }
}
