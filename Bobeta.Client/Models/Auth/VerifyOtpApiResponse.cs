namespace Bobeta.Client.Models.Auth;

/// <summary>JSON body from POST api/Auth/verify-otp (camelCase).</summary>
public sealed class VerifyOtpApiResponse
{
    public string? Token { get; set; }
    public Guid? PlayerId { get; set; }
    public string? PlayerName { get; set; }
}
