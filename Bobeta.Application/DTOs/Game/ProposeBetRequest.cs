namespace Bobeta.Application.DTOs.Game;

/// <summary>Request to propose a new bet amount for a game (opponent must accept).</summary>
public record ProposeBetRequest(Guid GameId, decimal Amount);
