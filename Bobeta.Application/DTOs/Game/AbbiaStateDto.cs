namespace Bobeta.Application.DTOs.Game;

public record AbbiaStateDto(
    int TokenCount,
    IReadOnlyList<bool>? MyTokens,
    IReadOnlyList<bool>? OpponentTokens,
    int? MyCarvedUp,
    int? OpponentCarvedUp,
    bool IHaveThrown,
    bool OpponentHasThrown,
    bool CanThrow);

public record AbbiaMoveRequest(Guid SessionId);
