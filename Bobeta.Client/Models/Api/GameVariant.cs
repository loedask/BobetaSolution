using System.Text.Json.Serialization;

namespace Bobeta.Client.Models.Api;

[JsonConverter(typeof(JsonStringEnumConverter<GameVariant>))]
public enum GameVariant
{
    Makopa = 0,
    Kopo = 1,
    Ngola = 2,
    Domino = 3,
    Abbia = 4,
    Nzengue = 5,
    Yote = 6
}
