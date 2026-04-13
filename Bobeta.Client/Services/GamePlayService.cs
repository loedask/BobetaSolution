using Bobeta.Client.Contracts;
using Bobeta.Client.Models.Games;
using Bobeta.Client.Services.Base;
using BaseApiException = Bobeta.Client.Services.Base.ApiException;

namespace Bobeta.Client.Services;

/// <summary>Client service for gameplay (start game, play card) using the NSwag-generated client.</summary>
public class GamePlayService(IClient client, HttpClient httpClient) : BaseHttpService(client, httpClient)
{
    public async Task<Response<bool>> StartGameAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            await Client.StartAsync(sessionId, cancellationToken).ConfigureAwait(false);
            return Response<bool>.Success(true);
        }
        catch (BaseApiException ex)
        {
            return Response<bool>.Failure(ex.Message, ex.StatusCode);
        }
    }

    public async Task<Response<GameStateViewModel?>> PlayCardAsync(Guid sessionId, GameMoveRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var card = new CardPlayDto
            {
                Suit = Enum.TryParse<CardSuit>(request.Suit, out var s) ? s : (CardSuit)int.Parse(request.Suit, System.Globalization.CultureInfo.InvariantCulture),
                Rank = (CardRank)request.Rank
            };
            var body = new PlayCardRequest { SessionId = sessionId, Card = card };
            var dto = await Client.PlayCardAsync(body, cancellationToken).ConfigureAwait(false);
            return Response<GameStateViewModel?>.Success(MapState(dto));
        }
        catch (BaseApiException ex)
        {
            return Response<GameStateViewModel?>.Failure(ex.Message, ex.StatusCode);
        }
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
            LobbyPotAmount = dto.LobbyPotAmount
        };
    }
}
