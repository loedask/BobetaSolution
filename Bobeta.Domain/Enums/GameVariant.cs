namespace Bobeta.Domain.Enums;

/// <summary>Which ruleset a game session uses.</summary>
public enum GameVariant
{
    /// <summary>Four-card trick-taking (current production game).</summary>
    Makopa = 0,

    /// <summary>10×10 international-style checkers (flying kings).</summary>
    Kopo = 1,

    /// <summary>Two-row, eight-pit Mancala game.</summary>
    Ngola = 2
}
