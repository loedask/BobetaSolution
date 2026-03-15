namespace Bobeta.Application.DTOs.Payment;

/// <summary>Request to initiate a MoMo withdrawal (disbursement).</summary>
public record PaymentWithdrawRequest(string PhoneNumber, decimal Amount);
