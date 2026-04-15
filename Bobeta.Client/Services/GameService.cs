using Bobeta.Client.Contracts;
using Bobeta.Client.Contracts.Interfaces;
using Bobeta.Client.Models.Games;
using Bobeta.Client.Services.Base;
using BaseApiException = Bobeta.Client.Services.Base.ApiException;
using CreateGameRequestDto = Bobeta.Client.Services.Base.CreateGameRequest;
using JoinGameRequestDto = Bobeta.Client.Services.Base.JoinGameRequest;

namespace Bobeta.Client.Services;

/// <summary>Client service for game operations (create, join, propose bet, accept bet) using the NSwag-generated client.</summary>
public class GameService(IClient client, HttpClient httpClient) : BaseHttpService(client, httpClient), IGameService
{
    public async Task<Response<GameSessionViewModel?>> CreateGameAsync(Bobeta.Client.Models.Games.CreateGameRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = new CreateGameRequestDto { BetAmount = (double)request.BetAmount };
            var dto = await Client.CreateAsync(body, cancellationToken).ConfigureAwait(false);
            return Response<GameSessionViewModel?>.Success(MapSession(dto));
        }
        catch (BaseApiException ex)
        {
            return Response<GameSessionViewModel?>.Failure(ex.Message, ex.StatusCode);
        }
    }

    public async Task<Response<GameSessionViewModel?>> JoinGameAsync(Bobeta.Client.Models.Games.JoinGameRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = new JoinGameRequestDto { GameId = request.GameId };
            var dto = await Client.JoinAsync(body, cancellationToken).ConfigureAwait(false);
            return Response<GameSessionViewModel?>.Success(MapSession(dto));
        }
        catch (BaseApiException ex)
        {
            return Response<GameSessionViewModel?>.Failure(ex.Message, ex.StatusCode);
        }
    }

    public async Task<Response<bool>> ProposeBetAsync(Guid gameId, double amount, CancellationToken cancellationToken = default)
    {
        try
        {
            await Client.ProposeBetAsync(new ProposeBetRequest { GameId = gameId, Amount = amount }, cancellationToken).ConfigureAwait(false);
            return Response<bool>.Success(true);
        }
        catch (BaseApiException ex)
        {
            return Response<bool>.Failure(ex.Message, ex.StatusCode);
        }
    }

    public async Task<Response<bool>> AcceptBetChangeAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        try
        {
            await Client.AcceptBetAsync(gameId, cancellationToken).ConfigureAwait(false);
            return Response<bool>.Success(true);
        }
        catch (BaseApiException ex)
        {
            return Response<bool>.Failure(ex.Message, ex.StatusCode);
        }
    }

    public async Task<Response<IReadOnlyList<GameSessionViewModel>>> GetOpenGamesAsync(CancellationToken cancellationToken = default)
    {
        var res = await GetAsync<List<GameSessionDto>>("api/Game/open?skip=0&take=100", cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccess || res.Data == null)
            return Response<IReadOnlyList<GameSessionViewModel>>.Failure(res.ErrorMessage ?? "Failed to load open games.", res.StatusCode);
        return Response<IReadOnlyList<GameSessionViewModel>>.Success(res.Data.Select(MapSession).ToList());
    }

    public async Task<Response<GameStateViewModel?>> GetGameStateAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = await Client.StateAsync(sessionId, cancellationToken).ConfigureAwait(false);
            return Response<GameStateViewModel?>.Success(MapState(dto));
        }
        catch (BaseApiException ex)
        {
            return Response<GameStateViewModel?>.Failure(ex.Message, ex.StatusCode);
        }
    }

    private static GameSessionViewModel MapSession(GameSessionDto dto)
    {
        return new GameSessionViewModel
        {
            Id = dto.Id,
            CreatorPlayerId = dto.CreatorPlayerId,
            OpponentPlayerId = dto.OpponentPlayerId,
            BetAmount = (decimal)dto.BetAmount,
            Status = dto.Status.ToString(),
            CreatedAt = dto.CreatedAt,
            StartedAt = dto.StartedAt,
            FinishedAt = dto.FinishedAt
        };
    }

    private static GameStateViewModel MapState(GameStateDto dto)
    {
        return new GameStateViewModel
        {
            SessionId = dto.SessionId,
            MyCards = dto.MyCards?.ToList() ?? new List<string>(),
            LastPlayedCard = dto.LastPlayedCard,
            CurrentTurnPlayerId = dto.CurrentTurnPlayerId,
            GameOver = dto.GameOver,
            WinnerPlayerId = dto.WinnerPlayerId,
            WaitingForGameStart = dto.WaitingForGameStart,
            LobbyPotAmount = (decimal)dto.LobbyPotAmount
        };
    }
}
