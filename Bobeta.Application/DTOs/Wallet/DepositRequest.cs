namespace Bobeta.Application.DTOs.Wallet;

/// <summary>Request to deposit an amount into the player's wallet.</summary>
public record DepositRequest(decimal Amount);
