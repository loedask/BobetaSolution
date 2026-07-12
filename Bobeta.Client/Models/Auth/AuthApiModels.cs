using System.Text.Json.Serialization;

namespace Bobeta.Client.Models.Auth;

public sealed class SendOtpApiRequest
{
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = "";
}

public sealed class VerifyOtpApiRequest
{
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = "";

    [JsonPropertyName("code")]
    public string Code { get; set; } = "";
}

public sealed class RegisterPlayerApiRequest
{
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = "";

    [JsonPropertyName("playerName")]
    public string PlayerName { get; set; } = "";
}

/// <summary>JSON body from POST api/Auth/register.</summary>
public sealed class AuthResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = "";

    [JsonPropertyName("playerId")]
    public Guid PlayerId { get; set; }

    [JsonPropertyName("playerName")]
    public string PlayerName { get; set; } = "";

    [JsonPropertyName("isNewUser")]
    public bool IsNewUser { get; set; }
}
