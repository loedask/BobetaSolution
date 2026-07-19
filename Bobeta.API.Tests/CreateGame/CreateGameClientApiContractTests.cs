using System.Text.Json;
using Bobeta.API.App.Json;
using Bobeta.Application.DTOs.Game;
using Bobeta.Client.Models.Api;
using Xunit;

namespace Bobeta.API.Tests.CreateGame;

/// <summary>Ensures the Blazor/mobile client payload matches what the API model binder accepts.</summary>
public sealed class CreateGameClientApiContractTests
{
  private static readonly JsonSerializerOptions ClientJson = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
  };

  private static readonly JsonSerializerOptions ApiJson = CreateApiJson();

  [Theory]
  [InlineData(GameVariant.Makopa)]
  [InlineData(GameVariant.Kopo)]
  [InlineData(GameVariant.Ngola)]
  [InlineData(GameVariant.Domino)]
  [InlineData(GameVariant.Abbia)]
  [InlineData(GameVariant.Nzengue)]
  public void ClientCreatePayload_DeserializesWithApiJsonOptions(GameVariant variant)
  {
    var clientJson = JsonSerializer.Serialize(
      new CreateGameApiRequest { BetAmount = 200, Variant = variant },
      ClientJson);

    var apiRequest = JsonSerializer.Deserialize<CreateGameRequest>(clientJson, ApiJson);

    Assert.NotNull(apiRequest);
    Assert.Equal(200m, apiRequest!.BetAmount);
    Assert.Equal(variant switch
    {
      GameVariant.Makopa => Domain.Enums.GameVariant.Makopa,
      GameVariant.Kopo => Domain.Enums.GameVariant.Kopo,
      GameVariant.Ngola => Domain.Enums.GameVariant.Ngola,
      GameVariant.Domino => Domain.Enums.GameVariant.Domino,
      GameVariant.Abbia => Domain.Enums.GameVariant.Abbia,
      GameVariant.Nzengue => Domain.Enums.GameVariant.Nzengue,
      _ => throw new ArgumentOutOfRangeException(nameof(variant))
    }, apiRequest.Variant);
  }

  private static JsonSerializerOptions CreateApiJson()
  {
    var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    ApiJsonSerializerOptions.Configure(options);
    return options;
  }
}
