using Bobeta.Application.Interfaces;

namespace Bobeta.Application.Services;

public class NotificationService : INotificationService
{
    public Task SendGameInviteAsync(Guid recipientPlayerId, Guid gameSessionId, string inviterName, decimal betAmount, CancellationToken cancellationToken = default)
    {
        // Delivered via SignalR in API layer when client is connected.
        return Task.CompletedTask;
    }

    public Task SendBetProposalAsync(Guid recipientPlayerId, Guid gameSessionId, decimal proposedAmount, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SendGameResultAsync(Guid playerId, Guid gameSessionId, bool won, decimal amount, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
