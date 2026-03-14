using Bobeta.Domain.Enums;

namespace Bobeta.Application.DTOs.History;

public record GameHistoryItemDto(
    Guid GameSessionId,
    decimal BetAmount,
    GameStatus Status,
    Guid? OpponentPlayerId,
    Guid? WinnerPlayerId,
    decimal? WonAmount,
    DateTime CreatedAt);
