using Bobeta.Client.Contracts;
using Bobeta.Client.Models.Api;
using Bobeta.Client.Models.Influencer;
using Bobeta.Client.Services.Base;

namespace Bobeta.Client.Services;

/// <summary>Influencer invite code API.</summary>
public class InfluencerService(HttpClient httpClient, IAccessTokenProvider? accessTokenProvider = null)
    : BaseHttpService(httpClient, accessTokenProvider)
{
  public async Task<Response<InfluencerCodeStatusViewModel?>> GetStatusAsync(CancellationToken cancellationToken = default)
  {
    var res = await GetAsync<InfluencerCodeStatusDto>("api/influencer/code", cancellationToken).ConfigureAwait(false);
    if (!res.IsSuccess || res.Data is null)
      return Response<InfluencerCodeStatusViewModel?>.Failure(res.ErrorMessage ?? "Failed to load invite status.", res.StatusCode);

    return Response<InfluencerCodeStatusViewModel?>.Success(Map(res.Data));
  }

  public async Task<Response<InfluencerCodeStatusViewModel?>> ApplyCodeAsync(string code, CancellationToken cancellationToken = default)
  {
    var res = await PostAsync<InfluencerCodeStatusDto>(
        "api/influencer/code",
        new ApplyInfluencerCodeApiRequest { Code = code },
        cancellationToken).ConfigureAwait(false);

    if (!res.IsSuccess || res.Data is null)
      return Response<InfluencerCodeStatusViewModel?>.Failure(res.ErrorMessage ?? "Could not apply invite code.", res.StatusCode);

    return Response<InfluencerCodeStatusViewModel?>.Success(Map(res.Data));
  }

  private static InfluencerCodeStatusViewModel Map(InfluencerCodeStatusDto dto) => new()
  {
    HasPendingCode = dto.HasPendingCode,
    Code = dto.Code,
    InfluencerName = dto.InfluencerName,
    DiscountPercent = dto.DiscountPercent
  };
}
