namespace Bobeta.Application.Games.Domino;

/// <summary>Persisted Domino match state (double-six draw game).</summary>
public sealed class DominoGameState
{
    /// <summary>Tiles in the creator's hand, each encoded as "high-low" with high &gt;= low.</summary>
    public List<string> CreatorHand { get; set; } = new();

    /// <summary>Tiles in the opponent's hand.</summary>
    public List<string> OpponentHand { get; set; } = new();

    /// <summary>Remaining face-down tiles.</summary>
    public List<string> Boneyard { get; set; } = new();

    /// <summary>Played line from left end to right end.</summary>
    public List<string> Chain { get; set; } = new();

    public int? LeftEnd { get; set; }
    public int? RightEnd { get; set; }
    public Guid? CurrentTurnPlayerId { get; set; }

    /// <summary>True until the opening tile is played; that tile must be the starter's highest double (or highest tile).</summary>
    public bool IsOpening { get; set; } = true;

    /// <summary>Required opening tile key for the starter (e.g. "6-6").</summary>
    public string? OpeningTile { get; set; }
}
