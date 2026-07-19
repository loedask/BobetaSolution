namespace Bobeta.Application.Games.Yote;

public sealed class YoteGameState
{
    /// <summary>Occupant per cell; null means empty. Length <see cref="YoteRules.CellCount"/>.</summary>
    public Guid?[] Cells { get; set; } = new Guid?[YoteRules.CellCount];

    public int CreatorInHand { get; set; } = YoteRules.PiecesPerPlayer;
    public int OpponentInHand { get; set; } = YoteRules.PiecesPerPlayer;
    public Guid? CurrentTurnPlayerId { get; set; }
}
