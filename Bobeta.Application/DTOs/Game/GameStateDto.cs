namespace Bobeta.Application.DTOs.Game;

public record GameStateDto(
    Guid SessionId,
    IReadOnlyList<string> MyCards,
    string? LastPlayedCard,
    Guid? CurrentTurnPlayerId,
    bool GameOver,
    Guid? WinnerPlayerId);
