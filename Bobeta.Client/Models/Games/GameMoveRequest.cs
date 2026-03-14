namespace Bobeta.Client.Models.Games;

/// <summary>Request to play a card. Placeholder for API request.</summary>
public class GameMoveRequest
{
    public string Suit { get; set; } = string.Empty;
    public int Rank { get; set; }
}
