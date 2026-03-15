using Bobeta.Application.DTOs.Payment;

namespace Bobeta.Application.Interfaces;

/// <summary>Application service for MTN MoMo payments: deposit (request-to-pay), withdrawal (disbursement), status, and callback handling.</summary>
public interface IPaymentService
{
    /// <summary>Creates a pending deposit (request-to-pay) for the player; returns the payment transaction (pending) and triggers MoMo prompt on payer phone.</summary>
    Task<PaymentTransactionDto> RequestDepositAsync(Guid playerId, string phoneNumber, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>Creates a pending withdrawal (disbursement) after validating wallet balance; sends transfer to MoMo.</summary>
    Task<PaymentTransactionDto> RequestWithdrawalAsync(Guid playerId, string phoneNumber, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>Checks the status of a payment transaction (by our transaction id) and optionally refreshes from MoMo.</summary>
    Task<PaymentTransactionDto?> CheckTransactionStatusAsync(Guid transactionId, CancellationToken cancellationToken = default);

    /// <summary>Handles MoMo callback notification: updates payment transaction and on successful deposit/withdrawal updates the wallet. Returns NotFound if no transaction exists; AlreadyProcessed if already completed (idempotency); Processed if updated.</summary>
    Task<CallbackHandleResult> HandleMoMoCallbackAsync(MoMoCallbackRequest callbackData, CancellationToken cancellationToken = default);
}
