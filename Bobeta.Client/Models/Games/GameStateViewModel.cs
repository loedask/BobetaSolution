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

    /// <summary>Winner of the last resolved trick (e.g. follower with no led suit); null after the next trick begins.</summary>
    public Guid? LastTrickWinnerPlayerId { get; set; }
}
