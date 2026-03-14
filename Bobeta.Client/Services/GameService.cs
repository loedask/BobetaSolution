using Bobeta.Client.Contracts;
using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Models.Games;
using Bobeta.Client.Services.Base;

namespace Bobeta.Client.Services;

/// <summary>Client service for game operations. Placeholder implementation; real API calls to be added.</summary>
public class GameService(IClient client, HttpClient httpClient) : BaseHttpService(client, httpClient), IGameService
{
    public Task<Response<GameSessionViewModel?>> CreateGameAsync(CreateGameRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Response<GameSessionViewModel?>.Failure("Not implemented", 501));
    }

    public Task<Response<GameSessionViewModel?>> JoinGameAsync(JoinGameRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Response<GameSessionViewModel?>.Failure("Not implemented", 501));
    }

    public Task<Response<GameStateViewModel?>> GetGameStateAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Response<GameStateViewModel?>.Failure("Not implemented", 501));
    }

    public Task<Response<GameStateViewModel?>> PlayCardAsync(Guid sessionId, GameMoveRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Response<GameStateViewModel?>.Failure("Not implemented", 501));
    }
}
