using Bobeta.Domain.Enums;

namespace Bobeta.Application.DTOs.Game;

public record PlayCardRequest(Guid SessionId, CardPlayDto Card);

public record CardPlayDto(CardSuit Suit, CardRank Rank);
