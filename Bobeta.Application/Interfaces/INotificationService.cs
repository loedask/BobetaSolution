namespace Bobeta.Application.Interfaces;

public interface INotificationService
{
    Task SendGameInviteAsync(Guid recipientPlayerId, Guid gameSessionId, string inviterName, decimal betAmount, CancellationToken cancellationToken = default);
    Task SendBetProposalAsync(Guid recipientPlayerId, Guid gameSessionId, decimal proposedAmount, CancellationToken cancellationToken = default);
    Task SendGameResultAsync(Guid playerId, Guid gameSessionId, bool won, decimal amount, CancellationToken cancellationToken = default);
}
