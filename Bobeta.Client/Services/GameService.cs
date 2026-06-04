using Bobeta.Client.Contracts;
using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Models.Api;
using Bobeta.Client.Models.Games;
using Bobeta.Client.Services.Base;

namespace Bobeta.Client.Services;

/// <summary>Game lobby API: create, join, bet, open games, state.</summary>
public class GameService(HttpClient httpClient, IAccessTokenProvider? accessTokenProvider = null)
    : BaseHttpService(httpClient, accessTokenProvider), IGameService
{
    public async Task<Response<GameSessionViewModel?>> CreateGameAsync(CreateGameRequest request, CancellationToken cancellationToken = default)
    {
        var postRes = await PostAsync<GameSessionDto>(
            "api/Game/create",
            new CreateGameApiRequest { BetAmount = (double)request.BetAmount, Variant = request.Variant },
            cancellationToken).ConfigureAwait(false);
        if (!postRes.IsSuccess || postRes.Data == null)
            return Response<GameSessionViewModel?>.Failure(postRes.ErrorMessage ?? "Failed to create game.", postRes.StatusCode);
        return Response<GameSessionViewModel?>.Success(GameStateMapper.ToViewModel(postRes.Data));
    }

    public async Task<Response<GameSessionViewModel?>> JoinGameAsync(JoinGameRequest request, CancellationToken cancellationToken = default)
    {
        var postRes = await PostAsync<GameSessionDto>(
            "api/Game/join",
            new JoinGameApiRequest { GameId = request.GameId },
            cancellationToken).ConfigureAwait(false);
        if (!postRes.IsSuccess || postRes.Data == null)
            return Response<GameSessionViewModel?>.Failure(postRes.ErrorMessage ?? "Failed to join game.", postRes.StatusCode);
        return Response<GameSessionViewModel?>.Success(GameStateMapper.ToViewModel(postRes.Data));
    }

    public async Task<Response<bool>> ProposeBetAsync(Guid gameId, double amount, CancellationToken cancellationToken = default)
    {
        var res = await PostAsync<object>(
            "api/Game/propose-bet",
            new ProposeBetApiRequest { GameId = gameId, Amount = amount },
            cancellationToken).ConfigureAwait(false);
        return res.IsSuccess
            ? Response<bool>.Success(true)
            : Response<bool>.Failure(res.ErrorMessage ?? "Failed to propose bet.", res.StatusCode);
    }

    public async Task<Response<bool>> AcceptBetChangeAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var res = await PostAsync<object>($"api/Game/accept-bet?gameId={gameId:D}", null, cancellationToken).ConfigureAwait(false);
        return res.IsSuccess
            ? Response<bool>.Success(true)
            : Response<bool>.Failure(res.ErrorMessage ?? "Failed to accept bet.", res.StatusCode);
    }

    public async Task<Response<IReadOnlyList<GameSessionViewModel>>> GetOpenGamesAsync(CancellationToken cancellationToken = default)
    {
        var res = await GetAsync<List<GameSessionDto>>("api/Game/open?skip=0&take=100", cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccess || res.Data == null)
            return Response<IReadOnlyList<GameSessionViewModel>>.Failure(res.ErrorMessage ?? "Failed to load open games.", res.StatusCode);
        return Response<IReadOnlyList<GameSessionViewModel>>.Success(res.Data.Select(GameStateMapper.ToViewModel).ToList());
    }

    public async Task<Response<GameStateViewModel?>> GetGameStateAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var res = await GetAsync<GameStateDto>(
            $"api/GamePlay/state?sessionId={sessionId:D}",
            retryOnTransientFailure: true,
            cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccess || res.Data == null)
            return Response<GameStateViewModel?>.Failure(res.ErrorMessage ?? "Failed to load game state.", res.StatusCode);
        return Response<GameStateViewModel?>.Success(GameStateMapper.ToViewModel(res.Data));
    }
}
