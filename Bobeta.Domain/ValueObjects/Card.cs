using Bobeta.Domain.Enums;

namespace Bobeta.Domain.ValueObjects;

public record Card(CardSuit Suit, CardRank Rank)
{
    public int RankValue => (int)Rank;

    public override string ToString() => $"{Rank} of {Suit}";
}
