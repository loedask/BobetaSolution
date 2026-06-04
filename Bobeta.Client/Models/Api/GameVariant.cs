using System.Text.Json.Serialization;

namespace Bobeta.Client.Models.Api;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GameVariant
{
    Makopa = 0,
    Kopo = 1
}
