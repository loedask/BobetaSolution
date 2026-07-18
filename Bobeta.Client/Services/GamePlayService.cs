using System.Globalization;
using Bobeta.Client.Contracts;
using Bobeta.Client.Models.Api;
using Bobeta.Client.Models.Games;
using Bobeta.Client.Services.Base;

namespace Bobeta.Client.Services;

/// <summary>Gameplay API: start, play card, take, inactivity.</summary>
public class GamePlayService(HttpClient httpClient, IAccessTokenProvider? accessTokenProvider = null)
    : BaseHttpService(httpClient, accessTokenProvider)
{
    public async Task<Response<bool>> StartGameAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var res = await PostAsync<object>($"api/GamePlay/start?sessionId={sessionId:D}", null, cancellationToken).ConfigureAwait(false);
        return res.IsSuccess
            ? Response<bool>.Success(true)
            : Response<bool>.Failure(res.ErrorMessage ?? "Failed to start game.", res.StatusCode);
    }

    public async Task<Response<GameStateViewModel?>> VoidFollowDrawAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var uri = $"api/GamePlay/void-follow?sessionId={sessionId:D}";
        var res = await PostAsync<GameStateDto>(uri, null, cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccess || res.Data == null)
            return Response<GameStateViewModel?>.Failure(res.ErrorMessage ?? "Could not take.", res.StatusCode);
        return Response<GameStateViewModel?>.Success(GameStateMapper.ToViewModel(res.Data));
    }

    public async Task<Response<bool>> ContinueInactivityAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var uri = $"api/GamePlay/inactivity/continue?sessionId={sessionId:D}";
        var res = await PostAsync<object>(uri, null, cancellationToken).ConfigureAwait(false);
        return res.IsSuccess
            ? Response<bool>.Success(true)
            : Response<bool>.Failure(res.ErrorMessage ?? "Could not continue.", res.StatusCode);
    }

    public async Task<Response<bool>> CancelInactivityAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var uri = $"api/GamePlay/inactivity/cancel?sessionId={sessionId:D}";
        var res = await PostAsync<object>(uri, null, cancellationToken).ConfigureAwait(false);
        return res.IsSuccess
            ? Response<bool>.Success(true)
            : Response<bool>.Failure(res.ErrorMessage ?? "Could not cancel game.", res.StatusCode);
    }

    public async Task<Response<bool>> ForfeitAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var uri = $"api/GamePlay/forfeit?sessionId={sessionId:D}";
        var res = await PostAsync<object>(uri, null, cancellationToken).ConfigureAwait(false);
        return res.IsSuccess
            ? Response<bool>.Success(true)
            : Response<bool>.Failure(res.ErrorMessage ?? "Could not forfeit game.", res.StatusCode);
    }

    public async Task<Response<GameStateViewModel?>> ApplyKopoMoveAsync(
        Guid sessionId,
        IReadOnlyList<KopoSquareDto> path,
        CancellationToken cancellationToken = default)
    {
        var body = new KopoMoveApiRequest { SessionId = sessionId, Path = path.ToList() };
        var res = await PostAsync<GameStateDto>("api/GamePlay/kopo/move", body, cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccess || res.Data == null)
            return Response<GameStateViewModel?>.Failure(res.ErrorMessage ?? "Failed to apply move.", res.StatusCode);
        return Response<GameStateViewModel?>.Success(GameStateMapper.ToViewModel(res.Data));
    }

    public async Task<Response<GameStateViewModel?>> ApplyNgolaMoveAsync(
        Guid sessionId,
        int pitIndex,
        CancellationToken cancellationToken = default)
    {
        var body = new NgolaMoveApiRequest { SessionId = sessionId, PitIndex = pitIndex };
        var res = await PostAsync<GameStateDto>("api/GamePlay/ngola/move", body, cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccess || res.Data == null)
            return Response<GameStateViewModel?>.Failure(res.ErrorMessage ?? "Failed to sow seeds.", res.StatusCode);
        return Response<GameStateViewModel?>.Success(GameStateMapper.ToViewModel(res.Data));
    }

    public async Task<Response<GameStateViewModel?>> ApplyDominoMoveAsync(
        Guid sessionId,
        string action,
        int? high = null,
        int? low = null,
        string? end = null,
        CancellationToken cancellationToken = default)
    {
        var body = new DominoMoveApiRequest
        {
            SessionId = sessionId,
            Action = action,
            High = high,
            Low = low,
            End = end
        };
        var res = await PostAsync<GameStateDto>("api/GamePlay/domino/move", body, cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccess || res.Data == null)
            return Response<GameStateViewModel?>.Failure(res.ErrorMessage ?? "Failed to apply Domino move.", res.StatusCode);
        return Response<GameStateViewModel?>.Success(GameStateMapper.ToViewModel(res.Data));
    }

    public async Task<Response<GameStateViewModel?>> ApplyAbbiaThrowAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var body = new AbbiaMoveApiRequest { SessionId = sessionId };
        var res = await PostAsync<GameStateDto>("api/GamePlay/abbia/throw", body, cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccess || res.Data == null)
            return Response<GameStateViewModel?>.Failure(res.ErrorMessage ?? "Failed to throw Abbia tokens.", res.StatusCode);
        return Response<GameStateViewModel?>.Success(GameStateMapper.ToViewModel(res.Data));
    }

    public async Task<Response<GameStateViewModel?>> PlayCardAsync(Guid sessionId, GameMoveRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = new PlayCardApiRequest
            {
                SessionId = sessionId,
                Card = new CardPlayDto
                {
                    Suit = ParseSuitForApi(request.Suit),
                    Rank = request.Rank
                }
            };
            var res = await PostAsync<GameStateDto>("api/GamePlay/play-card", body, cancellationToken).ConfigureAwait(false);
            if (!res.IsSuccess || res.Data == null)
                return Response<GameStateViewModel?>.Failure(res.ErrorMessage ?? "Failed to play card.", res.StatusCode);
            return Response<GameStateViewModel?>.Success(GameStateMapper.ToViewModel(res.Data));
        }
        catch (FormatException ex)
        {
            return Response<GameStateViewModel?>.Failure(ex.Message, null);
        }
    }

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
}
