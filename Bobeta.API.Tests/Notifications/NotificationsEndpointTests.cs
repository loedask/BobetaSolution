using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Bobeta.API.App.Json;
using Bobeta.API.Tests.Infrastructure;
using Bobeta.Application.DTOs.Notifications;
using Bobeta.Domain.Enums;
using Xunit;

namespace Bobeta.API.Tests.Notifications;

public sealed class NotificationsEndpointTests(BobetaApiFactory factory) : IClassFixture<BobetaApiFactory>
{
    private static readonly Guid TestPlayerId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private static readonly JsonSerializerOptions ApiJson = CreateApiJson();

    private static JsonSerializerOptions CreateApiJson()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ApiJsonSerializerOptions.Configure(options);
        return options;
    }

    [Fact]
    public async Task GetInbox_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/Notifications");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetInbox_ReturnsSeededItemsForPlayer()
    {
        using var client = AuthenticatedClient();
        var item = FakeNotificationService.CreateUnread(type: NotificationType.OpponentJoined, amount: 500m);
        factory.Notifications.Seed(item);

        using var response = await client.GetAsync("/api/Notifications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var inbox = await response.Content.ReadFromJsonAsync<List<NotificationDto>>(ApiJson);
        Assert.NotNull(inbox);
        Assert.Single(inbox!);
        Assert.Equal(item.Id, inbox[0].Id);
        Assert.Equal(NotificationType.OpponentJoined, inbox[0].Type);
        Assert.Equal(TestPlayerId, factory.Notifications.LastPlayerId);
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsUnreadOnly()
    {
        using var client = AuthenticatedClient();
        factory.Notifications.Seed(
            FakeNotificationService.CreateUnread(),
            FakeNotificationService.CreateUnread() with { IsRead = true });

        using var response = await client.GetAsync("/api/Notifications/unread-count");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var count = await response.Content.ReadFromJsonAsync<int>(ApiJson);
        Assert.Equal(1, count);
        Assert.Equal(TestPlayerId, factory.Notifications.LastPlayerId);
    }

    [Fact]
    public async Task MarkRead_ReturnsNoContentAndMarksItem()
    {
        using var client = AuthenticatedClient();
        var item = FakeNotificationService.CreateUnread();
        factory.Notifications.Seed(item);

        using var response = await client.PostAsync($"/api/Notifications/{item.Id}/read", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(item.Id, factory.Notifications.MarkReadCalls);
        Assert.True(factory.Notifications.Inbox.Single().IsRead);
    }

    [Fact]
    public async Task MarkAllRead_ReturnsNoContent()
    {
        using var client = AuthenticatedClient();
        factory.Notifications.Seed(
            FakeNotificationService.CreateUnread(),
            FakeNotificationService.CreateUnread());

        using var response = await client.PostAsync("/api/Notifications/read-all", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(TestPlayerId, factory.Notifications.MarkAllReadCalls);
        Assert.All(factory.Notifications.Inbox, n => Assert.True(n.IsRead));
    }

    private HttpClient AuthenticatedClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokens.ForPlayer(TestPlayerId));
        return client;
    }
}
