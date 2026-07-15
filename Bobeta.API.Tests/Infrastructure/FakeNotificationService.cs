using Bobeta.Application.DTOs.Notifications;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Enums;

namespace Bobeta.API.Tests.Infrastructure;

public sealed class FakeNotificationService : INotificationService
{
    public List<NotificationDto> Inbox { get; } = new();
    public List<Guid> MarkReadCalls { get; } = new();
    public List<Guid> MarkAllReadCalls { get; } = new();
    public Guid? LastPlayerId { get; private set; }

    public Task NotifyOpponentJoinedAsync(Guid creatorPlayerId, Guid gameSessionId, string opponentName, decimal betAmount, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyGameResultAsync(Guid playerId, Guid gameSessionId, bool won, decimal amount, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyPaymentAsync(Guid playerId, bool isDeposit, bool success, decimal amount, Guid? paymentTransactionId = null, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SendGameInviteAsync(Guid recipientPlayerId, Guid gameSessionId, string inviterName, decimal betAmount, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SendBetProposalAsync(Guid recipientPlayerId, Guid gameSessionId, decimal proposedAmount, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<NotificationDto>> GetInboxAsync(Guid playerId, int skip = 0, int take = 30, CancellationToken cancellationToken = default)
    {
        LastPlayerId = playerId;
        return Task.FromResult<IReadOnlyList<NotificationDto>>(
            Inbox.Where(n => true).Skip(skip).Take(take).ToList());
    }

    public Task<int> GetUnreadCountAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        LastPlayerId = playerId;
        return Task.FromResult(Inbox.Count(n => !n.IsRead));
    }

    public Task MarkReadAsync(Guid playerId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        LastPlayerId = playerId;
        MarkReadCalls.Add(notificationId);
        var item = Inbox.FirstOrDefault(n => n.Id == notificationId);
        if (item is null)
            return Task.CompletedTask;
        var idx = Inbox.IndexOf(item);
        Inbox[idx] = item with { IsRead = true };
        return Task.CompletedTask;
    }

    public Task MarkAllReadAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        LastPlayerId = playerId;
        MarkAllReadCalls.Add(playerId);
        for (var i = 0; i < Inbox.Count; i++)
            Inbox[i] = Inbox[i] with { IsRead = true };
        return Task.CompletedTask;
    }

    public void Seed(params NotificationDto[] items)
    {
        Inbox.Clear();
        Inbox.AddRange(items);
    }

    public static NotificationDto CreateUnread(
        Guid? id = null,
        NotificationType type = NotificationType.DepositSuccess,
        decimal amount = 100m) =>
        new(
            id ?? Guid.NewGuid(),
            type,
            ActorName: null,
            amount,
            RelatedEntityId: Guid.NewGuid(),
            DeepLink: "/dashboard",
            IsRead: false,
            CreatedAt: DateTime.UtcNow);
}
