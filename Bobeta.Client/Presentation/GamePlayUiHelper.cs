using Bobeta.Client.Models.Api;

namespace Bobeta.Client.Presentation;

/// <summary>Variant-aware UI rules for board games and Makopa's card table.</summary>
public static class GamePlayUiHelper
{
    public static bool ShowLoadingShell(
        bool isLoading,
        GameVariant variant,
        bool hasBoard,
        int handCardCount,
        bool waitingForOpponent) =>
        isLoading && !waitingForOpponent
        && (variant != GameVariant.Makopa ? !hasBoard : handCardCount == 0);

    public static bool IsMatchTableActive(
        bool gameOver,
        bool waitingForOpponent,
        GameVariant variant,
        bool hasBoard,
        int handCardCount) =>
        !gameOver && !waitingForOpponent
        && (variant != GameVariant.Makopa ? hasBoard : handCardCount > 0);
}
