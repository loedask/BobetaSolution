using Bobeta.Domain.Enums;

namespace Bobeta.Application.DTOs.Game;

/// <summary>Request to play a card in an active game session.</summary>
public record PlayCardRequest(Guid SessionId, CardPlayDto Card);

/// <summary>Suit and rank of the card being played.</summary>
public record CardPlayDto(CardSuit Suit, CardRank Rank);
