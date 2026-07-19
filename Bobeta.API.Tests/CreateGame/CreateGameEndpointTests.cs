using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bobeta.API.App.Json;
using Bobeta.API.Tests.Infrastructure;
using Bobeta.Application.DTOs.Game;
using Bobeta.Domain.Enums;
using Xunit;

namespace Bobeta.API.Tests.CreateGame;

public sealed class CreateGameEndpointTests(BobetaApiFactory factory) : IClassFixture<BobetaApiFactory>
{
  private static readonly Guid TestPlayerId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

  private static readonly JsonSerializerOptions ApiJson = CreateApiJson();

  private static JsonSerializerOptions CreateApiJson()
  {
    var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    ApiJsonSerializerOptions.Configure(options);
    return options;
  }

  [Theory]
  [InlineData("Makopa", GameVariant.Makopa)]
  [InlineData("Kopo", GameVariant.Kopo)]
  [InlineData("Ngola", GameVariant.Ngola)]
  [InlineData("Domino", GameVariant.Domino)]
  [InlineData("Abbia", GameVariant.Abbia)]
  [InlineData("Nzengue", GameVariant.Nzengue)]
  [InlineData("Yote", GameVariant.Yote)]
  public async Task PostCreate_AcceptsClientJsonWithStringVariant(string variantName, GameVariant expectedVariant)
  {
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwtTokens.ForPlayer(TestPlayerId));

    var body = $$"""{"betAmount":200,"variant":"{{variantName}}"}""";
    using var content = new StringContent(body, Encoding.UTF8, "application/json");
    using var response = await client.PostAsync("/api/Game/create", content);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var session = await response.Content.ReadFromJsonAsync<GameSessionDto>(ApiJson);
    Assert.NotNull(session);
    Assert.Equal(expectedVariant, session!.Variant);
    Assert.Equal(200m, session.BetAmount);
    Assert.Equal(TestPlayerId, session.CreatorPlayerId);
    Assert.Equal(TestPlayerId, FakeGameSessionService.LastCreatePlayerId);
    Assert.Equal(200m, FakeGameSessionService.LastCreateBetAmount);
    Assert.Equal(expectedVariant, FakeGameSessionService.LastCreateVariant);
  }

  [Fact]
  public async Task PostCreate_RejectsBetBelowMinimum()
  {
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwtTokens.ForPlayer(TestPlayerId));

    using var response = await client.PostAsJsonAsync("/api/Game/create", new { betAmount = 100, variant = "Makopa" });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task PostCreate_WithoutAuth_ReturnsUnauthorized()
  {
    using var client = factory.CreateClient();
    using var content = new StringContent("""{"betAmount":200,"variant":"Makopa"}""", Encoding.UTF8, "application/json");
    using var response = await client.PostAsync("/api/Game/create", content);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }
}
