using System.Net.Http;
using Bobeta.Client.Contracts;
using Bobeta.Client.Models.Auth;
using Bobeta.Client.Models.Players;
using Bobeta.Client.Services.Base;

namespace Bobeta.Client.Services;

/// <summary>Auth API: OTP, verify, register.</summary>
public class AuthService(HttpClient httpClient, IAccessTokenProvider? accessTokenProvider = null)
    : BaseHttpService(httpClient, accessTokenProvider)
{
    public async Task<Response<bool>> SendOtpAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await PostAsync<object>(
                "api/Auth/send-otp",
                new SendOtpApiRequest { PhoneNumber = phoneNumber },
                cancellationToken).ConfigureAwait(false);
            return res.IsSuccess
                ? Response<bool>.Success(true)
                : Response<bool>.Failure(res.ErrorMessage ?? "Failed to send code.", res.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            return Response<bool>.Failure(NetworkErrorMessage(ex), 0);
        }
        catch (TaskCanceledException)
        {
            return Response<bool>.Failure("Request timed out. Check that the API is running and the URL in appsettings.Development.json.", 0);
        }
        catch (Exception ex)
        {
            return Response<bool>.Failure(NetworkErrorMessage(ex), 0);
        }
    }

    public async Task<Response<VerifyOtpApiResponse>> VerifyOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        try
        {
            return await PostAsync<VerifyOtpApiResponse>(
                "api/Auth/verify-otp",
                new VerifyOtpApiRequest { PhoneNumber = phoneNumber, Code = code },
                cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            return Response<VerifyOtpApiResponse>.Failure(NetworkErrorMessage(ex), 0);
        }
        catch (TaskCanceledException)
        {
            return Response<VerifyOtpApiResponse>.Failure(
                "Request timed out. Check that the API is running and the URL in appsettings.Development.json.",
                0);
        }
        catch (Exception ex)
        {
            return Response<VerifyOtpApiResponse>.Failure(NetworkErrorMessage(ex), 0);
        }
    }

    public async Task<Response<AuthResponse>> RegisterAsync(CreatePlayerRequest request, CancellationToken cancellationToken = default)
    {
        var body = new RegisterPlayerApiRequest
        {
            PhoneNumber = request.PhoneNumber,
            PlayerName = request.PlayerName
        };
        var res = await PostAsync<AuthResponse>("api/Auth/register", body, cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccess || res.Data == null)
            return Response<AuthResponse>.Failure(res.ErrorMessage ?? "Registration failed.", res.StatusCode);
        return Response<AuthResponse>.Success(res.Data);
    }

    private static string NetworkErrorMessage(Exception ex)
    {
        var root = ex.GetBaseException();
        var inner = ex.InnerException;
        if (string.IsNullOrWhiteSpace(ex.Message))
            return !string.IsNullOrWhiteSpace(root.Message) ? root.Message : "Network error. Check your connection and ApiBaseUrl.";
        if (inner != null && !string.IsNullOrWhiteSpace(inner.Message) && !ex.Message.Contains(inner.Message, StringComparison.Ordinal))
            return $"{ex.Message} ({inner.Message})";
        return ex.Message;
    }
}
