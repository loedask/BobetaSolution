namespace Bobeta.Domain.Enums;

/// <summary>Current phase of a game session.</summary>
public enum GameStatus
{
    /// <summary>Created and waiting for an opponent to join.</summary>
    Waiting = 0,

    /// <summary>Two players joined; cards dealt and gameplay in progress.</summary>
    InProgress = 1,

    /// <summary>Game ended with a winner.</summary>
    Finished = 2,

    /// <summary>Game was cancelled (e.g. no opponent joined in time).</summary>
    Cancelled = 3
}
