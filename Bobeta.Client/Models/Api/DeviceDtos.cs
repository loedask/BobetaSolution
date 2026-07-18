using System.Text.Json.Serialization;
using Bobeta.Domain.Enums;

namespace Bobeta.Client.Models.Api;

public sealed class RegisterDeviceTokenApiRequest
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = "";

    [JsonPropertyName("platform")]
    public DevicePlatform Platform { get; set; }
}

public sealed class UnregisterDeviceTokenApiRequest
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = "";
}
