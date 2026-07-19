using System.Text.Json.Serialization;
using Bobeta.Client.Models.Api;
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
[JsonSerializable(typeof(KopoStateDto))]
[JsonSerializable(typeof(KopoPieceDto))]
[JsonSerializable(typeof(List<KopoPieceDto>))]
[JsonSerializable(typeof(NgolaStateDto))]
[JsonSerializable(typeof(DominoStateDto))]
[JsonSerializable(typeof(AbbiaStateDto))]
[JsonSerializable(typeof(NzengueStateDto))]
[JsonSerializable(typeof(NzengueEdgeDto))]
[JsonSerializable(typeof(List<NzengueEdgeDto>))]
[JsonSerializable(typeof(YoteStateDto))]
[JsonSerializable(typeof(YoteEdgeDto))]
[JsonSerializable(typeof(YoteCaptureDto))]
[JsonSerializable(typeof(List<YoteEdgeDto>))]
[JsonSerializable(typeof(List<YoteCaptureDto>))]
[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(List<bool>))]
[JsonSerializable(typeof(GameVariant))]
public partial class GameStateSignalRJsonContext : JsonSerializerContext;
