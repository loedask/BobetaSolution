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
            var list = await Client.PlayerAsync(skip, take, cancellationToken).ConfigureAwait(false);
            return Response<System.Collections.Generic.ICollection<GameHistoryItemDto>>.Success(list ?? new List<GameHistoryItemDto>());
        }
        catch (BaseApiException ex)
        {
            return Response<System.Collections.Generic.ICollection<GameHistoryItemDto>>.Failure(ex.Message, ex.StatusCode);
        }
    }
}
