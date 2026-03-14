using Bobeta.Domain.Enums;

namespace Bobeta.Application.DTOs.History;

/// <summary>Single game session entry in a player's history list.</summary>
public record GameHistoryItemDto(
    Guid GameSessionId,
    decimal BetAmount,
    GameStatus Status,
    Guid? OpponentPlayerId,
    Guid? WinnerPlayerId,
    decimal? WonAmount,
    DateTime CreatedAt);
