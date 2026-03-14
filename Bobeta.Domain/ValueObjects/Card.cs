using Bobeta.Domain.Enums;

namespace Bobeta.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a single playing card (suit and rank).
/// Used in the Makopa game engine and API DTOs.
/// </summary>
/// <param name="Suit">The card suit (Heart, Spade, Club, Diamond).</param>
/// <param name="Rank">The card rank (Two through Ace).</param>
public record Card(CardSuit Suit, CardRank Rank)
{
    /// <summary>Numeric rank value for comparison (2–14, Ace high).</summary>
    public int RankValue => (int)Rank;

    /// <inheritdoc />
    public override string ToString() => $"{Rank} of {Suit}";
}
