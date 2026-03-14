namespace Bobeta.Application.DTOs.Game;

/// <summary>Request to join an existing waiting game by id.</summary>
public record JoinGameRequest(Guid GameId);
