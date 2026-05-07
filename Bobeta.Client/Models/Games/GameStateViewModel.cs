namespace Bobeta.Client.Models.Games;

/// <summary>Current game state view model. Placeholder for API response.</summary>
public class GameStateViewModel
{
    public Guid SessionId { get; set; }
    public IReadOnlyList<string> MyCards { get; set; } = Array.Empty<string>();
    public string? LastPlayedCard { get; set; }
    public Guid? CurrentTurnPlayerId { get; set; }
    public bool GameOver { get; set; }
    public Guid? WinnerPlayerId { get; set; }

    public bool WaitingForGameStart { get; set; }

    public decimal LobbyPotAmount { get; set; }

    /// <summary>Other player's display name when the table has two seats; null while waiting for an opponent.</summary>
    public string? OpponentDisplayName { get; set; }

    /// <summary>Winner of the last resolved trick; null after the next trick begins.</summary>
    public Guid? LastTrickWinnerPlayerId { get; set; }

    public int MyRoundWins { get; set; }
    public int OpponentRoundWins { get; set; }

    /// <summary>From server: we are responding to opponent&apos;s lead and must follow suit (or Take if void).</summary>
    public bool MustFollowLedSuit { get; set; }
}
