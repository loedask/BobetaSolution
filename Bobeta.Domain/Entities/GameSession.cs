using Bobeta.Domain.Enums;

namespace Bobeta.Domain.Entities;

/// <summary>
/// A two-player game match. Tracks bet, variant rules, status, and serialized in-game state.
/// </summary>
public class GameSession
{
    /// <summary>Unique identifier for the game session.</summary>
    public Guid Id { get; set; }

    /// <summary>Player who created the game and set the bet amount.</summary>
    public Guid CreatorPlayerId { get; set; }

    /// <summary>Player who joined the game, or null while waiting for an opponent.</summary>
    public Guid? OpponentPlayerId { get; set; }

    /// <summary>Bet amount locked by each player for this game.</summary>
    public decimal BetAmount { get; set; }

    /// <summary>Ruleset for this session (Makopa, Kopo, …).</summary>
    public GameVariant Variant { get; set; } = GameVariant.Makopa;

    /// <summary>Current phase of the game (Waiting, InProgress, Finished, Cancelled).</summary>
    public GameStatus Status { get; set; }

    /// <summary>When the game was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the first card was dealt and gameplay started.</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>When the game ended (winner determined).</summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>JSON serialized variant-specific state (Makopa hands/tricks, Kopo board, …).</summary>
    public string? GameStateJson { get; set; }

    /// <summary>Navigation to the creator player.</summary>
    public Player CreatorPlayer { get; set; } = null!;

    /// <summary>Navigation to the opponent player, if joined.</summary>
    public Player? OpponentPlayer { get; set; }

    /// <summary>All card plays in this game, in order.</summary>
    public ICollection<GameMove> GameMoves { get; set; } = new List<GameMove>();

    /// <summary>Result record once the game has finished.</summary>
    public GameResult? GameResult { get; set; }
}
