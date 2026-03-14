using Bobeta.Client.Contracts;
using Bobeta.Client.Models.Games;

namespace Bobeta.Client.Contracts.Interfaces;

/// <summary>Client contract for game operations (create, join, play, state). Real API calls to be implemented.</summary>
public interface IGameService
{
    Task<Response<GameSessionViewModel?>> CreateGameAsync(CreateGameRequest request, CancellationToken cancellationToken = default);
    Task<Response<GameSessionViewModel?>> JoinGameAsync(JoinGameRequest request, CancellationToken cancellationToken = default);
    Task<Response<GameStateViewModel?>> GetGameStateAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<Response<GameStateViewModel?>> PlayCardAsync(Guid sessionId, GameMoveRequest request, CancellationToken cancellationToken = default);
}
