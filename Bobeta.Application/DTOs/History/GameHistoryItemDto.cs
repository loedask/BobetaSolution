using Bobeta.Domain.Enums;

namespace Bobeta.Application.DTOs.History;

/// <summary>Single game session entry in a player's history list.</summary>
/// <param name="IsCreator">True when this player created the session (hosted the lobby).</param>
public record GameHistoryItemDto(
    Guid GameSessionId,
    decimal BetAmount,
    GameStatus Status,
    GameVariant Variant,
    Guid? OpponentPlayerId,
    Guid? WinnerPlayerId,
    decimal? WonAmount,
    DateTime CreatedAt,
    bool IsCreator);
