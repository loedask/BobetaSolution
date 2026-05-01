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
    public static bool IsLegalFollowSuit(string cardToPlay, string? trickLeadSuit, IReadOnlyList<string> myHandCardStrings)
    {
        if (string.IsNullOrEmpty(trickLeadSuit))
            return true;
        var hasLeadSuit = myHandCardStrings.Any(c => c.StartsWith(trickLeadSuit, StringComparison.Ordinal));
        if (!hasLeadSuit)
            return true;
        return cardToPlay.StartsWith(trickLeadSuit, StringComparison.Ordinal);
    }
}
