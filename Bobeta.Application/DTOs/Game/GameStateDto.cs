namespace Bobeta.Application.DTOs.Game;

/// <summary>Current in-game state for a player: hand, last played card, whose turn, game over, winner.</summary>
/// <param name="WaitingForGameStart">True when the session exists but cards are not dealt yet (waiting for opponent or before start).</param>
/// <param name="LobbyPotAmount">Total pot display (e.g. both players' stakes); meaningful before and during play.</param>
public record GameStateDto(
    Guid SessionId,
    IReadOnlyList<string> MyCards,
    string? LastPlayedCard,
    Guid? CurrentTurnPlayerId,
    bool GameOver,
    Guid? WinnerPlayerId,
    bool WaitingForGameStart = false,
    decimal LobbyPotAmount = 0);
