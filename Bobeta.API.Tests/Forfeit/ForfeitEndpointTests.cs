using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Bobeta.API.App.Json;
using Bobeta.API.Tests.Infrastructure;
using Bobeta.Application.DTOs.Game;
using Bobeta.Domain.Enums;
using Xunit;

namespace Bobeta.API.Tests.Forfeit;

public sealed class ForfeitEndpointTests(BobetaApiFactory factory) : IClassFixture<BobetaApiFactory>
{
    private static readonly Guid TestPlayerId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid WinnerId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private static readonly JsonSerializerOptions ApiJson = CreateApiJson();

    private static JsonSerializerOptions CreateApiJson()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ApiJsonSerializerOptions.Configure(options);
        return options;
    }

    [Fact]
    public async Task PostForfeit_WhenOutcomeAvailable_ReturnsOkAndRecordsCaller()
    {
        var games = factory.GameSessions;
        games.ResetForfeitTracking();
        var sessionId = Guid.NewGuid();
        games.NextForfeitOutcome = new ForfeitGameOutcome(
            sessionId, WinnerId, TestPlayerId, 150m, GameVariant.Makopa);

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokens.ForPlayer(TestPlayerId));

        using var response = await client.PostAsync($"/api/GamePlay/forfeit?sessionId={sessionId:D}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TestPlayerId, games.LastForfeitLoserId);
        Assert.Equal(sessionId, games.LastForfeitSessionId);

        var body = await response.Content.ReadFromJsonAsync<ForfeitGameOutcome>(ApiJson);
        Assert.NotNull(body);
        Assert.Equal(WinnerId, body!.WinnerPlayerId);
        Assert.Equal(TestPlayerId, body.LoserPlayerId);
        Assert.Equal(150m, body.WinnerAmount);
        Assert.Equal(GameVariant.Makopa, body.Variant);
    }

    [Fact]
    public async Task PostForfeit_WhenCannotForfeit_ReturnsBadRequest()
    {
        var games = factory.GameSessions;
        games.ResetForfeitTracking();
        games.NextForfeitOutcome = null;
        var sessionId = Guid.NewGuid();

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokens.ForPlayer(TestPlayerId));

        using var response = await client.PostAsync($"/api/GamePlay/forfeit?sessionId={sessionId:D}", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(TestPlayerId, games.LastForfeitLoserId);
        Assert.Equal(sessionId, games.LastForfeitSessionId);
    }

    [Fact]
    public async Task PostForfeit_WithoutAuth_ReturnsUnauthorized()
    {
        var games = factory.GameSessions;
        games.ResetForfeitTracking();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync($"/api/GamePlay/forfeit?sessionId={Guid.NewGuid():D}", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(games.LastForfeitLoserId);
    }
}
