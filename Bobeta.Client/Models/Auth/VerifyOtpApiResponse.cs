using System.Text.Json.Serialization;

namespace Bobeta.Client.Models.Auth;

/// <summary>JSON body from POST api/Auth/verify-otp (camelCase).</summary>
public sealed class VerifyOtpApiResponse
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("playerId")]
    public Guid? PlayerId { get; set; }

    [JsonPropertyName("playerName")]
    public string? PlayerName { get; set; }
}
