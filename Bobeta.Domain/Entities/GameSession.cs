using Bobeta.Domain.Enums;

namespace Bobeta.Domain.Entities;

public class GameSession
{
    public Guid Id { get; set; }
    public Guid CreatorPlayerId { get; set; }
    public Guid? OpponentPlayerId { get; set; }
    public decimal BetAmount { get; set; }
    public GameStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    /// <summary>JSON serialized in-game state: hands, current turn, trick.</summary>
    public string? GameStateJson { get; set; }

    public Player CreatorPlayer { get; set; } = null!;
    public Player? OpponentPlayer { get; set; }
    public ICollection<GameMove> GameMoves { get; set; } = new List<GameMove>();
    public GameResult? GameResult { get; set; }
}
