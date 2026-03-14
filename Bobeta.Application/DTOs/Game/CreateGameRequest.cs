namespace Bobeta.Application.DTOs.Game;

/// <summary>Request to create a new game with the specified bet amount (within platform limits).</summary>
public record CreateGameRequest(decimal BetAmount);
