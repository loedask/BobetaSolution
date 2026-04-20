namespace Bobeta.Client.Presentation;

/// <summary>Mirrors <see cref="Bobeta.Application.Services.GameEngineService.PlayCardAsync"/> follow-suit rule so the UI does not offer illegal plays.</summary>
public static class MakopaFollowSuit
{
    /// <summary>True when this card may be submitted while responding to a lead (same rule as server).</summary>
    public static bool IsLegalPlay(string cardToPlay, string? lastPlayedCard, IReadOnlyList<string> myHandDisplayValues)
    {
        if (string.IsNullOrEmpty(lastPlayedCard))
            return true;
        var leadSuit = SuitPrefix(lastPlayedCard);
        if (string.IsNullOrEmpty(leadSuit))
            return true;
        var hasLeadSuit = myHandDisplayValues.Any(c => c.StartsWith(leadSuit, StringComparison.Ordinal));
        if (!hasLeadSuit)
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
