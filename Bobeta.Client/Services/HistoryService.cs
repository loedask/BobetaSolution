using Bobeta.Client.Contracts;
using Bobeta.Client.Services.Base;
using BaseApiException = Bobeta.Client.Services.Base.ApiException;

namespace Bobeta.Client.Services;

/// <summary>Client service for game history using the NSwag-generated client.</summary>
public class HistoryService(IClient client, HttpClient httpClient, IAccessTokenProvider? accessTokenProvider = null)
    : BaseHttpService(client, httpClient, accessTokenProvider)
{
    public async Task<Response<System.Collections.Generic.ICollection<GameHistoryItemDto>>> GetGameHistoryAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = $"api/History/player?skip={skip}&take={take}";
            var getRes = await GetAsync<List<GameHistoryItemDto>>(uri, cancellationToken).ConfigureAwait(false);
            if (!getRes.IsSuccess || getRes.Data == null)
                return Response<System.Collections.Generic.ICollection<GameHistoryItemDto>>.Failure(
                    getRes.ErrorMessage ?? "Failed to load game history.",
                    getRes.StatusCode);
            return Response<System.Collections.Generic.ICollection<GameHistoryItemDto>>.Success(getRes.Data);
        }
        catch (BaseApiException ex)
        {
            return Response<System.Collections.Generic.ICollection<GameHistoryItemDto>>.Failure(ex.Message, ex.StatusCode);
        }
    }
}
