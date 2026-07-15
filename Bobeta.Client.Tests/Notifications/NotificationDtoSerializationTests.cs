using System.Text.Json;
using System.Text.Json.Serialization;
using Bobeta.Client.Models.Notifications;
using Xunit;

namespace Bobeta.Client.Tests.Notifications;

public sealed class NotificationDtoSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void NotificationViewModel_DeserializesApiStringEnumPayload()
    {
        const string json = """
            {
              "id":"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
              "type":"OpponentJoined",
              "actorName":"Berta",
              "amount":500,
              "relatedEntityId":"11111111-2222-3333-4444-555555555555",
              "deepLink":"/game/11111111-2222-3333-4444-555555555555",
              "isRead":false,
              "createdAt":"2026-07-15T12:00:00Z"
            }
            """;

        var item = JsonSerializer.Deserialize<NotificationViewModel>(json, JsonOptions);

        Assert.NotNull(item);
        Assert.Equal(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), item!.Id);
        Assert.Equal("OpponentJoined", item.Type);
        Assert.Equal("Berta", item.ActorName);
        Assert.Equal(500m, item.Amount);
        Assert.Equal("/game/11111111-2222-3333-4444-555555555555", item.DeepLink);
        Assert.False(item.IsRead);
    }

    [Theory]
    [InlineData("GameWon")]
    [InlineData("GameLost")]
    [InlineData("DepositSuccess")]
    [InlineData("DepositFailed")]
    [InlineData("WithdrawSuccess")]
    [InlineData("WithdrawFailed")]
    public void NotificationViewModel_DeserializesKnownTypes(string type)
    {
        var json = $$"""{"id":"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee","type":"{{type}}","isRead":true,"createdAt":"2026-07-15T12:00:00Z"}""";
        var item = JsonSerializer.Deserialize<NotificationViewModel>(json, JsonOptions);
        Assert.NotNull(item);
        Assert.Equal(type, item!.Type);
        Assert.True(item.IsRead);
    }
}
