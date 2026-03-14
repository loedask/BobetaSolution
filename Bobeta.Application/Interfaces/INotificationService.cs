namespace Bobeta.Application.Interfaces;

/// <summary>Contract for sending notifications (game invite, bet proposal, game result). Implementations may use SignalR, push, or other channels.</summary>
public interface INotificationService
{
    /// <summary>Notifies the recipient that they have been invited to a game (inviter name, bet amount).</summary>
    Task SendGameInviteAsync(Guid recipientPlayerId, Guid gameSessionId, string inviterName, decimal betAmount, CancellationToken cancellationToken = default);

    /// <summary>Notifies the recipient of a bet change proposal for a game.</summary>
    Task SendBetProposalAsync(Guid recipientPlayerId, Guid gameSessionId, decimal proposedAmount, CancellationToken cancellationToken = default);

    /// <summary>Notifies a player of the outcome of a game (won/lost and amount).</summary>
    Task SendGameResultAsync(Guid playerId, Guid gameSessionId, bool won, decimal amount, CancellationToken cancellationToken = default);
}
