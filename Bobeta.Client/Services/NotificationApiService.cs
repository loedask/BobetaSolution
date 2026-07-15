using Bobeta.Client.Contracts;
using Bobeta.Client.Models.Api;
using Bobeta.Client.Models.Notifications;
using Bobeta.Client.Services.Base;

namespace Bobeta.Client.Services;

/// <summary>Player inbox API client.</summary>
public class NotificationApiService(HttpClient httpClient, IAccessTokenProvider? accessTokenProvider = null)
    : BaseHttpService(httpClient, accessTokenProvider)
{
    public async Task<Response<IReadOnlyList<NotificationViewModel>>> GetInboxAsync(
        int skip = 0,
        int take = 30,
        CancellationToken cancellationToken = default)
    {
        var res = await GetAsync<List<NotificationApiDto>>($"api/Notifications?skip={skip}&take={take}", cancellationToken)
            .ConfigureAwait(false);
        if (!res.IsSuccess || res.Data is null)
            return Response<IReadOnlyList<NotificationViewModel>>.Failure(
                res.ErrorMessage ?? "Failed to load notifications.",
                res.StatusCode);
        return Response<IReadOnlyList<NotificationViewModel>>.Success(res.Data.Select(Map).ToList());
    }

    public async Task<Response<int>> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var res = await GetAsync<int>("api/Notifications/unread-count", cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccess)
            return Response<int>.Failure(res.ErrorMessage ?? "Failed to load unread count.", res.StatusCode);
        return Response<int>.Success(res.Data);
    }

    public Task<Response<object?>> MarkReadAsync(Guid id, CancellationToken cancellationToken = default) =>
        PostAsync<object?>("api/Notifications/" + id + "/read", new { }, cancellationToken);

    public Task<Response<object?>> MarkAllReadAsync(CancellationToken cancellationToken = default) =>
        PostAsync<object?>("api/Notifications/read-all", new { }, cancellationToken);

    private static NotificationViewModel Map(NotificationApiDto dto) => new()
    {
        Id = dto.Id,
        Type = string.IsNullOrWhiteSpace(dto.Type) ? "Unknown" : dto.Type,
        ActorName = dto.ActorName,
        Amount = dto.Amount,
        RelatedEntityId = dto.RelatedEntityId,
        DeepLink = dto.DeepLink,
        IsRead = dto.IsRead,
        CreatedAt = dto.CreatedAt
    };
}
