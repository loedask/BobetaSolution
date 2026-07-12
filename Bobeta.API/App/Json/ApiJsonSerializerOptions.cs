using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bobeta.API.App.Json;

/// <summary>Shared System.Text.Json settings for MVC input/output formatters.</summary>
public static class ApiJsonSerializerOptions
{
    public static void Configure(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter());
    }
}
