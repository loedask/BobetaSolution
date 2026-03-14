namespace Bobeta.Application.Interfaces;

/// <summary>Contract for Mobile Money (MoMo) operations: request/confirm deposit, send withdrawal. Implementations integrate with provider APIs.</summary>
public interface IMobileMoneyService
{
    /// <summary>Initiates a deposit request for the given phone and amount. Returns a reference for confirmation.</summary>
    Task<MobileMoneyResult> RequestDepositAsync(string phoneNumber, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>Confirms a deposit by reference (e.g. after user completed payment).</summary>
    Task<MobileMoneyResult> ConfirmDepositAsync(string reference, CancellationToken cancellationToken = default);

    /// <summary>Initiates a withdrawal to the given phone number for the amount.</summary>
    Task<MobileMoneyResult> SendWithdrawalAsync(string phoneNumber, decimal amount, CancellationToken cancellationToken = default);
}

/// <summary>Result of a Mobile Money operation: success flag, optional reference, and optional message.</summary>
public record MobileMoneyResult(bool Success, string? Reference, string? Message);
