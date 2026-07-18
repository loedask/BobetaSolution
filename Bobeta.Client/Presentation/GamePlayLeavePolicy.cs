namespace Bobeta.Client.Presentation;

/// <summary>Shared leave/forfeit confirm rules for Web and Mobile game play screens.</summary>
public static class GamePlayLeavePolicy
{
    /// <summary>
    /// True when the player is in an active match and must confirm that leaving forfeits the pot.
    /// </summary>
    public static bool ShouldConfirmLeave(
        bool allowLeaveWithoutForfeit,
        bool leaveInProgress,
        bool showGameResult,
        bool waitingForOpponent,
        string? sessionId,
        bool hasPlayerId) =>
        !allowLeaveWithoutForfeit
        && !leaveInProgress
        && !showGameResult
        && !waitingForOpponent
        && !string.IsNullOrEmpty(sessionId)
        && hasPlayerId;
}
