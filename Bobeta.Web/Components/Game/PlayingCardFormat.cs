using Bobeta.Web.ViewModels.Games;

namespace Bobeta.Web.Components.Game;

/// <summary>Maps API card strings (e.g. <c>Spade_3</c>, <c>Heart_14</c>) to display labels and suit symbols for the UI.</summary>
public static class PlayingCardFormat
{
    /// <summary>Returns rank corner label, center suit symbol, and whether the suit uses red ink.</summary>
    public static (string RankLabel, string SuitSymbol, bool IsRed) Resolve(CardViewModel? card)
    {
        if (card == null)
            return ("?", "?", false);

        var symbol = SuitSymbol(card.Suit, out var isRed);
        return (RankLabel(card.Rank), symbol, isRed);
    }

    public static string RankLabel(string rank)
    {
        if (int.TryParse(rank.Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var r))
        {
            return r switch
            {
                11 => "J",
                12 => "Q",
                13 => "K",
                14 => "A",
                _ => r.ToString(System.Globalization.CultureInfo.InvariantCulture)
            };
        }

        return string.IsNullOrEmpty(rank) ? "?" : rank.Trim();
    }

    public static string SuitSymbol(string suit, out bool isRed)
    {
        isRed = false;
        if (string.IsNullOrWhiteSpace(suit))
            return "?";

        var s = suit.Trim();

        if (int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var n))
        {
            // CardSuit: Heart=0, Spade=1, Club=2, Diamond=3
            return n switch
            {
                0 => SetRed(out isRed, "♥"),
                1 => "♠",
                2 => "♣",
                3 => SetRed(out isRed, "♦"),
                _ => "?"
            };
        }

        if (s.Equals("Heart", StringComparison.OrdinalIgnoreCase))
            return SetRed(out isRed, "♥");
        if (s.Equals("Spade", StringComparison.OrdinalIgnoreCase))
            return "♠";
        if (s.Equals("Club", StringComparison.OrdinalIgnoreCase))
            return "♣";
        if (s.Equals("Diamond", StringComparison.OrdinalIgnoreCase))
            return SetRed(out isRed, "♦");

        return "?";
    }

    private static string SetRed(out bool isRed, string symbol)
    {
        isRed = true;
        return symbol;
    }
}
