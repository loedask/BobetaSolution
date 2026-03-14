namespace Bobeta.Application.DTOs.Game;

/// <summary>Result of a finished game: winner, loser, pot split, commission.</summary>
public record GameResultDto(
    Guid GameSessionId,
    Guid WinnerPlayerId,
    Guid LoserPlayerId,
    decimal TotalPot,
    decimal WinnerAmount,
    decimal PlatformCommission);
