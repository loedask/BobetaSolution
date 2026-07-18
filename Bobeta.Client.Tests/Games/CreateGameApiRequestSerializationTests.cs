using System.Text.Json;
using Bobeta.Client.Models.Api;
using Xunit;

namespace Bobeta.Client.Tests.Games;

public sealed class CreateGameApiRequestSerializationTests
{
  private static readonly JsonSerializerOptions ClientJsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
  };

  [Theory]
  [InlineData(GameVariant.Makopa, "Makopa")]
  [InlineData(GameVariant.Kopo, "Kopo")]
  [InlineData(GameVariant.Ngola, "Ngola")]
  public void SerializeCreateGameApiRequest_WritesStringVariant(GameVariant variant, string expectedVariantName)
  {
    var request = new CreateGameApiRequest { BetAmount = 200, Variant = variant };

    var json = JsonSerializer.Serialize(request, ClientJsonOptions);
    using var document = JsonDocument.Parse(json);

    Assert.Equal(200, document.RootElement.GetProperty("betAmount").GetDouble());
    Assert.Equal(expectedVariantName, document.RootElement.GetProperty("variant").GetString());
  }

  [Fact]
  public void SerializeCreateGameApiRequest_DoesNotWriteIntegerVariant()
  {
    var request = new CreateGameApiRequest { BetAmount = 300, Variant = GameVariant.Kopo };

    var json = JsonSerializer.Serialize(request, ClientJsonOptions);

    Assert.DoesNotContain("\"variant\":1", json);
    Assert.Contains("\"variant\":\"Kopo\"", json);
  }
}
