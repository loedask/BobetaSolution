namespace Bobeta.Application.Games.Kopo;

public class KopoGameState
{
    public List<KopoPiece> Pieces { get; set; } = new();
    public Guid? CurrentTurnPlayerId { get; set; }
    /// <summary>During a multi-jump, the piece that must continue capturing.</summary>
    public int? ChainPieceId { get; set; }
    public int NextPieceId { get; set; } = 1;
}

public class KopoPiece
{
    public int Id { get; set; }
    public Guid OwnerId { get; set; }
    public int Row { get; set; }
    public int Col { get; set; }
    public bool IsKing { get; set; }
}
