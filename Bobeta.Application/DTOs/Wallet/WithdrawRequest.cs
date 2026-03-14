namespace Bobeta.Application.DTOs.Wallet;

/// <summary>Request to withdraw an amount from the player's wallet.</summary>
public record WithdrawRequest(decimal Amount);
