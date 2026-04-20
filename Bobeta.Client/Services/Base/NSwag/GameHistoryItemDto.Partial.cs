using System.Text.Json.Serialization;

namespace Bobeta.Client.Services.Base;

public partial class GameHistoryItemDto
{
    /// <summary>True when the current player created this session (API: <c>isCreator</c>).</summary>
    [JsonPropertyName("isCreator")]
    public bool IsCreator { get; set; }
}
