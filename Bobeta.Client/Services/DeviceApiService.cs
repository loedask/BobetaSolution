using Bobeta.Client.Contracts;
using Bobeta.Client.Models.Api;
using Bobeta.Client.Services.Base;
using Bobeta.Domain.Enums;

namespace Bobeta.Client.Services;

/// <summary>Registers FCM device tokens with the API for phone push.</summary>
public class DeviceApiService(HttpClient httpClient, IAccessTokenProvider? accessTokenProvider = null)
    : BaseHttpService(httpClient, accessTokenProvider)
{
    public async Task<Response<bool>> RegisterAsync(string token, DevicePlatform platform, CancellationToken cancellationToken = default)
    {
        var res = await PostAsync<object>(
            "api/Devices/register",
            new RegisterDeviceTokenApiRequest { Token = token, Platform = platform },
            cancellationToken).ConfigureAwait(false);
        return res.IsSuccess
            ? Response<bool>.Success(true)
            : Response<bool>.Failure(res.ErrorMessage ?? "Failed to register device.", res.StatusCode);
    }

    public async Task<Response<bool>> UnregisterAsync(string token, CancellationToken cancellationToken = default)
    {
        var res = await PostAsync<object>(
            "api/Devices/unregister",
            new UnregisterDeviceTokenApiRequest { Token = token },
            cancellationToken).ConfigureAwait(false);
        return res.IsSuccess
            ? Response<bool>.Success(true)
            : Response<bool>.Failure(res.ErrorMessage ?? "Failed to unregister device.", res.StatusCode);
    }
}
