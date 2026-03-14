namespace Bobeta.Application.Interfaces;

public interface IMobileMoneyService
{
    Task<MobileMoneyResult> RequestDepositAsync(string phoneNumber, decimal amount, CancellationToken cancellationToken = default);
    Task<MobileMoneyResult> ConfirmDepositAsync(string reference, CancellationToken cancellationToken = default);
    Task<MobileMoneyResult> SendWithdrawalAsync(string phoneNumber, decimal amount, CancellationToken cancellationToken = default);
}

public record MobileMoneyResult(bool Success, string? Reference, string? Message);
