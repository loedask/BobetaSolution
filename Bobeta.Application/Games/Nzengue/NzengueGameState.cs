namespace Bobeta.Application.Games.Nzengue;

public sealed class NzengueGameState
{
    /// <summary>Occupant player id per board point; null means empty. Length <see cref="NzengueRules.PointCount"/>.</summary>
    public Guid?[] Points { get; set; } = new Guid?[NzengueRules.PointCount];

    public int CreatorPiecesToPlace { get; set; } = NzengueRules.PiecesPerPlayer;
    public int OpponentPiecesToPlace { get; set; } = NzengueRules.PiecesPerPlayer;
    public Guid? CurrentTurnPlayerId { get; set; }
}
