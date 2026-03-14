namespace Bobeta.Application.DTOs.Game;

public record GameResultDto(
    Guid GameSessionId,
    Guid WinnerPlayerId,
    Guid LoserPlayerId,
    decimal TotalPot,
    decimal WinnerAmount,
    decimal PlatformCommission);
