using Bobeta.Client.Contracts;
using Bobeta.Client.Models.Players;
using Bobeta.Client.Services.Base;

namespace Bobeta.Client.Services;

/// <summary>Client service for auth (send OTP, verify, register). Placeholder; implement when API calls are required.</summary>
public class AuthService(IClient client, HttpClient httpClient) : BaseHttpService(client, httpClient)
{
    public Task<Response<bool>> SendOtpAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Response<bool>.Failure("Not implemented", 501));
    }

    public Task<Response<string?>> VerifyOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Response<string?>.Failure("Not implemented", 501));
    }

    public Task<Response<PlayerViewModel?>> RegisterAsync(CreatePlayerRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Response<PlayerViewModel?>.Failure("Not implemented", 501));
    }
}
