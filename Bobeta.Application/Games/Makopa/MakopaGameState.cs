namespace Bobeta.Application.Games.Makopa;

/// <summary>
/// In-memory state for a Makopa game. Serialized to JSON in <see cref="Bobeta.Domain.Entities.GameSession.GameStateJson"/>.
/// </summary>
public class MakopaGameState
{
    public List<string> CreatorHand { get; set; } = new();
    public List<string> OpponentHand { get; set; } = new();
    public Guid? CurrentTurnPlayerId { get; set; }
    public Guid? LeadPlayerId { get; set; }
    public string? TrickSuit { get; set; }
    public List<PlayedInTrick> TrickPlays { get; set; } = new();
    public Guid? LastTrickWinnerPlayerId { get; set; }
    public int CreatorRoundWins { get; set; }
    public int OpponentRoundWins { get; set; }
}

public class PlayedInTrick
{
    public Guid PlayerId { get; set; }
    public string Card { get; set; } = string.Empty;
}
