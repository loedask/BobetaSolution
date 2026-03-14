namespace Bobeta.Application.Common;

/// <summary>In-memory state for a Makopa game. Serialized to JSON in GameSession.GameStateJson.</summary>
public class MakopaGameState
{
    public List<string> CreatorHand { get; set; } = new();
    public List<string> OpponentHand { get; set; } = new();
    public Guid? CurrentTurnPlayerId { get; set; }
    public Guid? LeadPlayerId { get; set; }
    public string? TrickSuit { get; set; }
    public List<PlayedInTrick> TrickPlays { get; set; } = new();
}

public class PlayedInTrick
{
    public Guid PlayerId { get; set; }
    public string Card { get; set; } = string.Empty;
}
