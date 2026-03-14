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
}
