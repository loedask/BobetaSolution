using Bobeta.Client.Models.Api;

namespace Bobeta.Client.Presentation;

/// <summary>Variant-aware UI rules for game play loading and inactivity (Kopo has no hand cards).</summary>
public static class GamePlayUiHelper
{
    public static bool ShowLoadingShell(
        bool isLoading,
        GameVariant variant,
        bool hasKopoBoard,
        int handCardCount,
        bool waitingForOpponent) =>
        isLoading && !waitingForOpponent
        && (variant == GameVariant.Kopo ? !hasKopoBoard : handCardCount == 0);

    public static bool IsMatchTableActive(
        bool gameOver,
        bool waitingForOpponent,
        GameVariant variant,
        bool hasKopoBoard,
        int handCardCount) =>
        !gameOver && !waitingForOpponent
        && (variant == GameVariant.Kopo ? hasKopoBoard : handCardCount > 0);
}
