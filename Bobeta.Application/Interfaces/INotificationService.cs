using Bobeta.Application.DTOs.Notifications;

namespace Bobeta.Application.Interfaces;

/// <summary>Creates, queries, and delivers in-app player notifications.</summary>
public interface INotificationService
{
    Task NotifyOpponentJoinedAsync(
        Guid creatorPlayerId,
        Guid gameSessionId,
        string opponentName,
        decimal betAmount,
        CancellationToken cancellationToken = default);

    Task NotifyGameResultAsync(
        Guid playerId,
        Guid gameSessionId,
        bool won,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task NotifyPaymentAsync(
        Guid playerId,
        bool isDeposit,
        bool success,
        decimal amount,
        Guid? paymentTransactionId = null,
        CancellationToken cancellationToken = default);

    Task SendGameInviteAsync(
        Guid recipientPlayerId,
        Guid gameSessionId,
        string inviterName,
        decimal betAmount,
        CancellationToken cancellationToken = default);

    Task SendBetProposalAsync(
        Guid recipientPlayerId,
        Guid gameSessionId,
        decimal proposedAmount,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> GetInboxAsync(
        Guid playerId,
        int skip = 0,
        int take = 30,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid playerId, CancellationToken cancellationToken = default);

    Task MarkReadAsync(Guid playerId, Guid notificationId, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(Guid playerId, CancellationToken cancellationToken = default);
}
