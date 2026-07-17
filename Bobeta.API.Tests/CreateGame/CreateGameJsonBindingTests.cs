using System.Text.Json;
using Bobeta.API.App.Json;
using Bobeta.Application.DTOs.Game;
using Bobeta.Domain.Enums;
using Xunit;

namespace Bobeta.API.Tests.CreateGame;

public sealed class CreateGameJsonBindingTests
{
  [Theory]
  [InlineData("Makopa", GameVariant.Makopa)]
  [InlineData("Kopo", GameVariant.Kopo)]
  [InlineData("Ngola", GameVariant.Ngola)]
  public void DeserializeCreateGameRequest_AcceptsStringVariant(string variantName, GameVariant expected)
  {
    var options = CreateApiOptions();
    var json = $$"""{"betAmount":200,"variant":"{{variantName}}"}""";

    var request = JsonSerializer.Deserialize<CreateGameRequest>(json, options);

    Assert.NotNull(request);
    Assert.Equal(200m, request!.BetAmount);
    Assert.Equal(expected, request.Variant);
  }

  [Theory]
  [InlineData(0, GameVariant.Makopa)]
  [InlineData(1, GameVariant.Kopo)]
  [InlineData(2, GameVariant.Ngola)]
  public void DeserializeCreateGameRequest_AcceptsIntegerVariant(int variantValue, GameVariant expected)
  {
    var options = CreateApiOptions();
    var json = $$"""{"betAmount":300,"variant":{{variantValue}}}""";

    var request = JsonSerializer.Deserialize<CreateGameRequest>(json, options);

    Assert.NotNull(request);
    Assert.Equal(expected, request!.Variant);
  }

  [Fact]
  public void DeserializeCreateGameRequest_WithoutStringEnumConverter_FailsOnClientPayload()
  {
    var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    var json = """{"betAmount":200,"variant":"Makopa"}""";

    Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CreateGameRequest>(json, options));
  }

  private static JsonSerializerOptions CreateApiOptions()
  {
    var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    ApiJsonSerializerOptions.Configure(options);
    return options;
  }
}
