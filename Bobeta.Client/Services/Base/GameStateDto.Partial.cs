using System.Text.Json.Serialization;

namespace Bobeta.Client.Services.Base;

/// <summary>Extends the NSwag-generated <see cref="GameStateDto"/> with fields added on the server after code generation.</summary>
public partial class GameStateDto
{
    [JsonPropertyName("waitingForGameStart")]
    public bool WaitingForGameStart { get; set; }

    [JsonPropertyName("lobbyPotAmount")]
    public decimal LobbyPotAmount { get; set; }
}
