namespace Bobeta.Application.Games.Ngola;

public sealed class NgolaGameState
{
    public int[] Pits { get; set; } = new int[NgolaRules.TotalPits];
    public int CreatorScore { get; set; }
    public int OpponentScore { get; set; }
    public Guid? CurrentTurnPlayerId { get; set; }
}
