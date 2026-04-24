using System.Text.Json.Serialization;

namespace Bobeta.Client.Services.Base;

public partial class GameStateDto
{
    /// <summary>Winner of the most recently completed trick; omitted once the next trick starts.</summary>
    [JsonPropertyName("lastTrickWinnerPlayerId")]
    public Guid? LastTrickWinnerPlayerId { get; set; }
}
