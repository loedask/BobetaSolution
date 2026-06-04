using Bobeta.Client.Models.Api;
using Bobeta.Client.Models.Games;

namespace Bobeta.Client.Presentation;

/// <summary>Maps server game snapshots onto table UI state (single code path for HTTP and SignalR).</summary>
public static class GamePlayStateApplier
{
    public static bool IsSessionEndedWithoutWinner(GameStateViewModel state) =>
        state.GameOver && !state.WinnerPlayerId.HasValue && !state.WaitingForGameStart;

    public static ApplyResult ApplyAuthoritativeState(
        GamePlayTableState table,
        GameStateViewModel state,
        Guid? myPlayerId,
        bool blockInteraction,
        Func<string>? trickOutcomeYou = null,
        Func<string>? trickOutcomeOpponent = null,
        Func<int, int, string>? roundScoreFormat = null)
    {
        if (IsSessionEndedWithoutWinner(state))
            return ApplyResult.SessionEnded;

        table.Variant = state.Variant;
        table.Kopo = state.Kopo;
        table.WaitingForOpponent = state.WaitingForGameStart;
        table.PotAmount = state.LobbyPotAmount;
        table.OpponentDisplayName = state.OpponentDisplayName;
        table.CurrentPlayerId = state.CurrentTurnPlayerId;
        table.IsPlayerTurn = !table.WaitingForOpponent && state.CurrentTurnPlayerId == myPlayerId;
        if (state.Variant == GameVariant.Kopo)
        {
            table.PlayerCards = new List<CardViewModel>();
            table.LastPlayedCard = null;
            table.MustFollowLedSuit = false;
            table.CanTakeCard = false;
        }
        else
        {
            table.PlayerCards = ParseCards(state.MyCards ?? new List<string>());
            table.LastPlayedCard = string.IsNullOrEmpty(state.LastPlayedCard) ? null : ParseCard(state.LastPlayedCard);
            table.MustFollowLedSuit = state.MustFollowLedSuit;
        }

        if (state.GameOver)
        {
            table.ShowGameResult = true;
            table.WinnerPlayerName = state.WinnerPlayerId == myPlayerId ? "You" : "Opponent";
        }
        else
            table.ShowGameResult = false;

        table.TrickOutcomeMessage = FormatTrickOutcome(
            state.LastTrickWinnerPlayerId, myPlayerId, trickOutcomeYou, trickOutcomeOpponent);
        ApplyMatchRoundScore(table, state, roundScoreFormat);
        if (state.Variant != GameVariant.Kopo)
            RefreshHandPlayability(table, blockInteraction);
        return state.GameOver ? ApplyResult.GameOver : ApplyResult.Applied;
    }

    public static void RefreshHandPlayability(GamePlayTableState table, bool blockInteraction)
    {
        var canInteract = table.IsPlayerTurn && !table.WaitingForOpponent && !table.ShowGameResult && !blockInteraction;
        var last = table.LastPlayedCard?.DisplayValue;
        var enforceFollow = table.MustFollowLedSuit && !string.IsNullOrEmpty(last);
        var hand = table.PlayerCards.Select(c => c.DisplayValue).ToList();
        var needsVoid = canInteract && enforceFollow && MakopaFollowSuit.ResponderNeedsVoidFollow(last, hand);
        foreach (var c in table.PlayerCards)
        {
            if (!canInteract)
                c.IsPlayable = true;
            else if (needsVoid)
                c.IsPlayable = false;
            else if (enforceFollow)
                c.IsPlayable = MakopaFollowSuit.IsLegalPlay(c.DisplayValue, last, hand);
            else
                c.IsPlayable = true;
        }

        table.CanTakeCard = needsVoid;
    }

    public static List<CardViewModel> ParseCards(IEnumerable<string> cards) =>
        cards.Select(ParseCard).ToList();

    public static CardViewModel ParseCard(string value)
    {
        var parts = value.Split('-', '_');
        var suit = parts.Length > 0 ? parts[0] : "0";
        var rank = parts.Length > 1 ? parts[1] : value;
        return new CardViewModel
        {
            Suit = suit,
            Rank = rank,
            DisplayValue = value,
            CssClass = ""
        };
    }

    private static void ApplyMatchRoundScore(
        GamePlayTableState table,
        GameStateViewModel state,
        Func<int, int, string>? roundScoreFormat)
    {
        if (state.WaitingForGameStart)
        {
            table.MyRoundWins = table.OpponentRoundWins = 0;
            table.MatchRoundScoreText = null;
            return;
        }

        table.MyRoundWins = state.MyRoundWins;
        table.OpponentRoundWins = state.OpponentRoundWins;
        if (table.MyRoundWins == 0 && table.OpponentRoundWins == 0)
            table.MatchRoundScoreText = null;
        else
            table.MatchRoundScoreText = roundScoreFormat?.Invoke(table.MyRoundWins, table.OpponentRoundWins)
                ?? $"Hands won: {table.MyRoundWins}\u2013{table.OpponentRoundWins}";
    }

    private static string? FormatTrickOutcome(
        Guid? lastTrickWinnerPlayerId,
        Guid? myPlayerId,
        Func<string>? trickOutcomeYou,
        Func<string>? trickOutcomeOpponent)
    {
        if (lastTrickWinnerPlayerId == null)
            return null;
        if (lastTrickWinnerPlayerId == myPlayerId)
            return trickOutcomeYou?.Invoke() ?? "You took this trick.";
        return trickOutcomeOpponent?.Invoke() ?? "Opponent took this trick.";
    }

    public enum ApplyResult
    {
        Applied,
        GameOver,
        SessionEnded
    }
}
