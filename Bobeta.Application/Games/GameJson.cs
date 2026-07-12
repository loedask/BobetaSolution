using System.Text.Json;

namespace Bobeta.Application.Games;

internal static class GameJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
