using System.Globalization;
using Bobeta.Client.Contracts;
using Bobeta.Client.Models.Games;
using Bobeta.Client.Services.Base;
using BaseApiException = Bobeta.Client.Services.Base.ApiException;

namespace Bobeta.Client.Services;

/// <summary>Client service for gameplay (start game, play card) using the NSwag-generated client.</summary>
public class GamePlayService(IClient client, HttpClient httpClient, IAccessTokenProvider? accessTokenProvider = null)
    : BaseHttpService(client, httpClient, accessTokenProvider)
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
            // NSwag CardSuit is _0.._3 (numeric); UI uses domain names Heart, Spade, Club, Diamond from API card strings.
            var suitInt = ParseSuitForApi(request.Suit);
            var card = new CardPlayDto
            {
                Suit = (CardSuit)suitInt,
                Rank = (CardRank)request.Rank
            };
            var body = new PlayCardRequest { SessionId = sessionId, Card = card };
            var res = await PostAsync<GameStateDto>("api/GamePlay/play-card", body, cancellationToken).ConfigureAwait(false);
            if (!res.IsSuccess || res.Data == null)
                return Response<GameStateViewModel?>.Failure(res.ErrorMessage ?? "Failed to play card.", res.StatusCode);
            return Response<GameStateViewModel?>.Success(MapState(res.Data));
        }
        catch (FormatException ex)
        {
            return Response<GameStateViewModel?>.Failure(ex.Message, null);
        }
    }

    /// <summary>Maps hand/card suit strings to 0–3 for JSON matching server <see cref="Bobeta.Domain.Enums.CardSuit"/>.</summary>
    private static int ParseSuitForApi(string? suit)
    {
        if (string.IsNullOrWhiteSpace(suit))
            throw new FormatException("Card suit is required.");
        var t = suit.Trim();
        if (int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n is >= 0 and <= 3)
            return n;
        return t.ToLowerInvariant() switch
        {
            "heart" => 0,
            "spade" => 1,
            "club" => 2,
            "diamond" => 3,
            _ => throw new FormatException($"Unknown card suit: {suit}")
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
            LobbyPotAmount = (decimal)dto.LobbyPotAmount,
            OpponentDisplayName = string.IsNullOrEmpty(dto.OpponentDisplayName) ? null : dto.OpponentDisplayName
        };
    }
}
