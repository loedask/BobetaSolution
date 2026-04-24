namespace Bobeta.Application.DTOs.Game;

/// <summary>Current in-game state for a player: hand, last played card, whose turn, game over, winner.</summary>
/// <param name="WaitingForGameStart">True when the session exists but cards are not dealt yet (waiting for opponent or before start).</param>
/// <param name="LobbyPotAmount">Total pot display (e.g. both players' stakes); meaningful before and during play.</param>
/// <param name="OpponentDisplayName">Other seat's display name when the session has two players; null while waiting.</param>
/// <param name="LastTrickWinnerPlayerId">Populated after a trick is taken (including when the follower had no led suit); cleared when the next trick starts.</param>
public record GameStateDto(
    Guid SessionId,
    IReadOnlyList<string> MyCards,
    string? LastPlayedCard,
    Guid? CurrentTurnPlayerId,
    bool GameOver,
    Guid? WinnerPlayerId,
    bool WaitingForGameStart = false,
    decimal LobbyPotAmount = 0,
    string? OpponentDisplayName = null,
    Guid? LastTrickWinnerPlayerId = null);
