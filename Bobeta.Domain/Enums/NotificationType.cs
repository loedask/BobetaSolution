namespace Bobeta.Domain.Enums;

/// <summary>In-app notification categories shown in the player inbox.</summary>
public enum NotificationType
{
    OpponentJoined = 0,
    GameWon = 1,
    GameLost = 2,
    DepositSuccess = 3,
    DepositFailed = 4,
    WithdrawSuccess = 5,
    WithdrawFailed = 6,
    GameInvite = 7,
    BetProposal = 8
}
