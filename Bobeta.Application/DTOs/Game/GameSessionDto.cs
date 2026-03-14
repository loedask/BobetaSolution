using Bobeta.Domain.Enums;

namespace Bobeta.Application.DTOs.Game;

public record GameSessionDto(
    Guid Id,
    Guid CreatorPlayerId,
    Guid? OpponentPlayerId,
    decimal BetAmount,
    GameStatus Status,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? FinishedAt);
