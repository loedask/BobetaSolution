using Bobeta.Client.Contracts;
using Bobeta.Client.Models.Api;
using Bobeta.Client.Services.Base;

namespace Bobeta.Client.Services;

/// <summary>Player game history API.</summary>
public class HistoryService(HttpClient httpClient, IAccessTokenProvider? accessTokenProvider = null)
    : BaseHttpService(httpClient, accessTokenProvider)
{
    public async Task<Response<IReadOnlyList<GameHistoryItemDto>>> GetGameHistoryAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        var uri = $"api/History/player?skip={skip}&take={take}";
        var getRes = await GetAsync<List<GameHistoryItemDto>>(uri, cancellationToken).ConfigureAwait(false);
        if (!getRes.IsSuccess || getRes.Data == null)
            return Response<IReadOnlyList<GameHistoryItemDto>>.Failure(
                getRes.ErrorMessage ?? "Failed to load game history.",
                getRes.StatusCode);
        return Response<IReadOnlyList<GameHistoryItemDto>>.Success(getRes.Data);
    }
}
