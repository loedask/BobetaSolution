using Bobeta.Client.Contracts;
using Bobeta.Client.Models.Api;
using Bobeta.Client.Models.Games;

namespace Bobeta.Client.Contracts.Interfaces;

/// <summary>Client contract for game operations (create, join, propose bet, accept bet, state, open games).</summary>
public interface IGameService
{
    Task<Response<GameSessionViewModel?>> CreateGameAsync(CreateGameRequest request, CancellationToken cancellationToken = default);
    Task<Response<GameSessionViewModel?>> JoinGameAsync(JoinGameRequest request, CancellationToken cancellationToken = default);
    Task<Response<bool>> ProposeBetAsync(Guid gameId, double amount, CancellationToken cancellationToken = default);
    Task<Response<bool>> AcceptBetChangeAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<Response<IReadOnlyList<GameSessionViewModel>>> GetOpenGamesAsync(GameVariant? variant = null, CancellationToken cancellationToken = default);
    Task<Response<IReadOnlyList<GameSessionViewModel>>> GetMyWaitingGamesAsync(GameVariant? variant = null, CancellationToken cancellationToken = default);
    Task<Response<bool>> CancelWaitingGameAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<Response<GameStateViewModel?>> GetGameStateAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
