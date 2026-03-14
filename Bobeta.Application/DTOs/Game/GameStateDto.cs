namespace Bobeta.Application.DTOs.Game;

/// <summary>Current in-game state for a player: hand, last played card, whose turn, game over, winner.</summary>
public record GameStateDto(
    Guid SessionId,
    IReadOnlyList<string> MyCards,
    string? LastPlayedCard,
    Guid? CurrentTurnPlayerId,
    bool GameOver,
    Guid? WinnerPlayerId);
