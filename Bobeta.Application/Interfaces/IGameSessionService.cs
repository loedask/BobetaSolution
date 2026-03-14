using Bobeta.Application.DTOs.Game;

namespace Bobeta.Application.Interfaces;

public interface IGameSessionService
{
    Task<GameSessionDto> CreateGameAsync(Guid playerId, decimal betAmount, CancellationToken cancellationToken = default);
    Task<GameSessionDto?> JoinGameAsync(Guid playerId, Guid gameId, CancellationToken cancellationToken = default);
    Task ProposeNewBetAsync(Guid playerId, Guid gameId, decimal amount, CancellationToken cancellationToken = default);
    Task AcceptBetChangeAsync(Guid gameId, CancellationToken cancellationToken = default);
}
