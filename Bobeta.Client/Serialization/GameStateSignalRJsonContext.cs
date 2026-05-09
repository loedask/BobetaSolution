using System.Text.Json.Serialization;
using Bobeta.Client.Models.Games;

namespace Bobeta.Client.Serialization;

/// <summary>
/// Source-generated JSON contract for SignalR <c>GameState</c> payloads (camelCase from API).
/// Avoids reflection-only deserialization failures under Blazor WASM trimming.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(GameStateViewModel))]
[JsonSerializable(typeof(List<string>))]
public partial class GameStateSignalRJsonContext : JsonSerializerContext;
