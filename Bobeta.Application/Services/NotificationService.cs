using Bobeta.Application.DTOs.Notifications;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Bobeta.Application.Services;

/// <summary>Persists inbox notifications, publishes over SignalR, and sends phone push when tokens exist.</summary>
public class NotificationService(
    IPlayerNotificationRepository repository,
    INotificationRealtimePublisher realtimePublisher,
    IPushNotificationSender pushSender,
    IPlayerDeviceTokenRepository deviceTokens,
    ILogger<NotificationService> logger) : INotificationService
{
    public Task NotifyOpponentJoinedAsync(
        Guid creatorPlayerId,
        Guid gameSessionId,
        string opponentName,
        decimal betAmount,
        CancellationToken cancellationToken = default) =>
        CreateAndPublishAsync(
            creatorPlayerId,
            NotificationType.OpponentJoined,
            opponentName,
            betAmount,
            gameSessionId,
            $"/game/{gameSessionId}",
            cancellationToken);

    public Task NotifyGameResultAsync(
        Guid playerId,
        Guid gameSessionId,
        bool won,
        decimal amount,
        CancellationToken cancellationToken = default) =>
        CreateAndPublishAsync(
            playerId,
            won ? NotificationType.GameWon : NotificationType.GameLost,
            actorName: null,
            amount,
            gameSessionId,
            "/history",
            cancellationToken);

    public Task NotifyPaymentAsync(
        Guid playerId,
        bool isDeposit,
        bool success,
        decimal amount,
        Guid? paymentTransactionId = null,
        CancellationToken cancellationToken = default)
    {
        var type = (isDeposit, success) switch
        {
            (true, true) => NotificationType.DepositSuccess,
            (true, false) => NotificationType.DepositFailed,
            (false, true) => NotificationType.WithdrawSuccess,
            _ => NotificationType.WithdrawFailed
        };
        return CreateAndPublishAsync(
            playerId,
            type,
            actorName: null,
            amount,
            paymentTransactionId,
            "/dashboard",
            cancellationToken);
    }

    public Task SendGameInviteAsync(
        Guid recipientPlayerId,
        Guid gameSessionId,
        string inviterName,
        decimal betAmount,
        CancellationToken cancellationToken = default) =>
        CreateAndPublishAsync(
            recipientPlayerId,
            NotificationType.GameInvite,
            inviterName,
            betAmount,
            gameSessionId,
            "/join",
            cancellationToken);

    public Task SendBetProposalAsync(
        Guid recipientPlayerId,
        Guid gameSessionId,
        decimal proposedAmount,
        CancellationToken cancellationToken = default) =>
        CreateAndPublishAsync(
            recipientPlayerId,
            NotificationType.BetProposal,
            actorName: null,
            proposedAmount,
            gameSessionId,
            $"/game/{gameSessionId}",
            cancellationToken);

    public async Task<IReadOnlyList<NotificationDto>> GetInboxAsync(
        Guid playerId,
        int skip = 0,
        int take = 30,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);
        skip = Math.Max(0, skip);
        var items = await repository.GetForPlayerAsync(playerId, skip, take, cancellationToken);
        return items.Select(Map).ToList();
    }

    public Task<int> GetUnreadCountAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        repository.CountUnreadAsync(playerId, cancellationToken);

    public async Task MarkReadAsync(Guid playerId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var item = await repository.GetByIdAsync(notificationId, cancellationToken);
        if (item is null || item.PlayerId != playerId || item.IsRead)
            return;
        item.IsRead = true;
        await repository.UpdateAsync(item, cancellationToken);
    }

    public Task MarkAllReadAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        repository.MarkAllReadAsync(playerId, cancellationToken);

    private async Task CreateAndPublishAsync(
        Guid playerId,
        NotificationType type,
        string? actorName,
        decimal? amount,
        Guid? relatedEntityId,
        string? deepLink,
        CancellationToken cancellationToken)
    {
        try
        {
            var entity = new PlayerNotification
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                Type = type,
                ActorName = actorName,
                Amount = amount,
                RelatedEntityId = relatedEntityId,
                DeepLink = deepLink,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await repository.AddAsync(entity, cancellationToken);
            var dto = Map(entity);
            await realtimePublisher.PublishAsync(playerId, dto, cancellationToken);
            await TrySendPushAsync(playerId, type, actorName, amount, relatedEntityId, deepLink, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create/publish notification type={Type} player={PlayerId}", type, playerId);
        }
    }

    private async Task TrySendPushAsync(
        Guid playerId,
        NotificationType type,
        string? actorName,
        decimal? amount,
        Guid? relatedEntityId,
        string? deepLink,
        CancellationToken cancellationToken)
    {
        try
        {
            var tokens = await deviceTokens.GetByPlayerIdAsync(playerId, cancellationToken);
            if (tokens.Count == 0)
                return;

            var (title, body) = FormatPush(type, actorName, amount);
            var data = new Dictionary<string, string>
            {
                ["type"] = type.ToString(),
                ["deepLink"] = deepLink ?? ""
            };
            if (relatedEntityId is { } id)
                data["relatedEntityId"] = id.ToString("D");

            var invalid = await pushSender.SendAsync(
                tokens.Select(t => t.Token).ToList(),
                title,
                body,
                data,
                cancellationToken);

            if (invalid.Count > 0)
                await deviceTokens.DeleteByTokensAsync(invalid, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send push notification type={Type} player={PlayerId}", type, playerId);
        }
    }

    /// <summary>Push copy is French-first (default market); inbox i18n still localizes in-app.</summary>
    private static (string Title, string Body) FormatPush(NotificationType type, string? actorName, decimal? amount)
    {
        var actor = string.IsNullOrWhiteSpace(actorName) ? "Adversaire" : actorName.Trim();
        var amountText = amount is { } a ? a.ToString("N0") : "";
        return type switch
        {
            NotificationType.OpponentJoined => ("Adversaire a rejoint", $"{actor} a rejoint votre partie de {amountText} FCFA."),
            NotificationType.GameWon => ("Victoire", $"Vous avez gagne {amountText} FCFA."),
            NotificationType.GameLost => ("Partie terminee", "Vous avez perdu cette partie."),
            NotificationType.DepositSuccess => ("Depot reussi", $"{amountText} FCFA ajoutes a votre portefeuille."),
            NotificationType.DepositFailed => ("Depot echoue", "Le depot n'a pas abouti."),
            NotificationType.WithdrawSuccess => ("Retrait reussi", $"{amountText} FCFA envoyes."),
            NotificationType.WithdrawFailed => ("Retrait echoue", "Le retrait n'a pas abouti."),
            NotificationType.GameInvite => ("Invitation", $"{actor} vous invite a une partie de {amountText} FCFA."),
            NotificationType.BetProposal => ("Nouvelle mise", $"Proposition de mise: {amountText} FCFA."),
            _ => ("Bobeta", "Vous avez une nouvelle notification.")
        };
    }

    private static NotificationDto Map(PlayerNotification n) =>
        new(n.Id, n.Type, n.ActorName, n.Amount, n.RelatedEntityId, n.DeepLink, n.IsRead, n.CreatedAt);
}
