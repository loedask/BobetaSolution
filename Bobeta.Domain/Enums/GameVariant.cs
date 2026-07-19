namespace Bobeta.Domain.Enums;

/// <summary>Which ruleset a game session uses.</summary>
public enum GameVariant
{
    /// <summary>Four-card trick-taking (current production game).</summary>
    Makopa = 0,

    /// <summary>10×10 international-style checkers (flying kings).</summary>
    Kopo = 1,

    /// <summary>Two-row, eight-pit Mancala game.</summary>
    Ngola = 2,

    /// <summary>Double-six Domino draw game (1v1).</summary>
    Domino = 3,

    /// <summary>1v1 Abbia token-flip chance game.</summary>
    Abbia = 4,

    /// <summary>Congo alignment game: place then move stones on a 9-point board.</summary>
    Nzengue = 5,

    /// <summary>West African 5×6 capture game (place, slide, jump + bonus remove).</summary>
    Yote = 6
}
