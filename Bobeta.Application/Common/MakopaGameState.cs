namespace Bobeta.Application.Common;

/// <summary>
/// In-memory state for a Makopa game. Serialized to JSON in <see cref="Bobeta.Domain.Entities.GameSession.GameStateJson"/>.
/// Tracks each player's hand (card strings "Suit_Rank"), whose turn it is, current trick, and lead player.
/// </summary>
public class MakopaGameState
{
    /// <summary>Cards in the creator's hand (format "Suit_Rank", e.g. "Heart_14").</summary>
    public List<string> CreatorHand { get; set; } = new();

    /// <summary>Cards in the opponent's hand.</summary>
    public List<string> OpponentHand { get; set; } = new();

    /// <summary>Player who must play next.</summary>
    public Guid? CurrentTurnPlayerId { get; set; }

    /// <summary>Player who led the current trick (and will lead the next if they win the trick).</summary>
    public Guid? LeadPlayerId { get; set; }

    /// <summary>Suit of the first card played in the current trick (null before first play).</summary>
    public string? TrickSuit { get; set; }

    /// <summary>Cards played in the current trick (0, 1, or 2 entries).</summary>
    public List<PlayedInTrick> TrickPlays { get; set; } = new();

    /// <summary>After a trick is resolved, set to the winner until the next trick begins (first card of next trick clears this).</summary>
    public Guid? LastTrickWinnerPlayerId { get; set; }

    /// <summary>Hands (rounds) won by the creator in this match.</summary>
    public int CreatorRoundWins { get; set; }

    /// <summary>Hands (rounds) won by the opponent in this match.</summary>
    public int OpponentRoundWins { get; set; }

    /// <summary>Remaining stock (deterministic order). Drawing when void on follow restores lead and adds one stock card to responder.</summary>
    public List<string> ReserveDeck { get; set; } = new();
}

/// <summary>A single card play within the current trick (player and card string).</summary>
public class PlayedInTrick
{
    /// <summary>Player who played the card.</summary>
    public Guid PlayerId { get; set; }

    /// <summary>Card played ("Suit_Rank").</summary>
    public string Card { get; set; } = string.Empty;
}
