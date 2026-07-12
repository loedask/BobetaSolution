using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bobeta.Client.Json;

/// <summary>Shared System.Text.Json settings for API request/response bodies in the client layer.</summary>
public static class ClientJsonSerializerOptions
{
    public static JsonSerializerOptions Create() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
