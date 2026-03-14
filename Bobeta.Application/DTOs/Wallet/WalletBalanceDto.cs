namespace Bobeta.Application.DTOs.Wallet;

/// <summary>Current wallet balances returned to the client.</summary>
public record WalletBalanceDto(decimal Balance, decimal LockedBalance);
