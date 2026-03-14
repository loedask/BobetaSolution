using Bobeta.Domain.Enums;

namespace Bobeta.Application.DTOs.Game;

/// <summary>Game session summary returned to clients (create, join, list).</summary>
public record GameSessionDto(
    Guid Id,
    Guid CreatorPlayerId,
    Guid? OpponentPlayerId,
    decimal BetAmount,
    GameStatus Status,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? FinishedAt);
