using System.Net.Http;
using Bobeta.Client.Contracts;
using Bobeta.Client.Models.Auth;
using Bobeta.Client.Models.Players;
using Bobeta.Client.Services.Base;
using BaseApiException = Bobeta.Client.Services.Base.ApiException;

namespace Bobeta.Client.Services;

/// <summary>Client service for auth (send OTP, verify, register) using the NSwag-generated client.</summary>
public class AuthService(IClient client, HttpClient httpClient) : BaseHttpService(client, httpClient)
{
    public async Task<Response<bool>> SendOtpAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            await Client.SendOtpAsync(new SendOtpRequest { PhoneNumber = phoneNumber }, cancellationToken).ConfigureAwait(false);
            return Response<bool>.Success(true);
        }
        catch (BaseApiException ex)
        {
            var detail = !string.IsNullOrWhiteSpace(ex.Response)
                ? ex.Response
                : ex.Message;
            return Response<bool>.Failure(detail.Trim(), ex.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            return Response<bool>.Failure(
                ex.Message.Length > 0 ? ex.Message : "Network error. Check your connection and API URL.",
                0);
        }
    }

    public async Task<Response<VerifyOtpApiResponse>> VerifyOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        try
        {
            return await PostAsync<VerifyOtpApiResponse>(
                "api/Auth/verify-otp",
                new VerifyOtpRequest { PhoneNumber = phoneNumber, Code = code },
                cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            return Response<VerifyOtpApiResponse>.Failure(
                ex.Message.Length > 0 ? ex.Message : "Network error. Check your connection and API URL.",
                0);
        }
    }

    public async Task<Response<AuthResponse>> RegisterAsync(CreatePlayerRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = new RegisterPlayerRequest
            {
                PhoneNumber = request.PhoneNumber,
                PlayerName = request.PlayerName
            };
            var result = await Client.RegisterAsync(body, cancellationToken).ConfigureAwait(false);
            return Response<AuthResponse>.Success(result);
        }
        catch (BaseApiException ex)
        {
            return Response<AuthResponse>.Failure(ex.Message, ex.StatusCode);
        }
    }
}
