namespace Bobeta.Domain.Entities;

public class GameResult
{
    public Guid Id { get; set; }
    public Guid GameSessionId { get; set; }
    public Guid WinnerPlayerId { get; set; }
    public Guid LoserPlayerId { get; set; }
    public decimal TotalPot { get; set; }
    public decimal WinnerAmount { get; set; }
    public decimal PlatformCommission { get; set; }
    public DateTime CreatedAt { get; set; }

    public GameSession GameSession { get; set; } = null!;
    public Player WinnerPlayer { get; set; } = null!;
    public Player LoserPlayer { get; set; } = null!;
}
