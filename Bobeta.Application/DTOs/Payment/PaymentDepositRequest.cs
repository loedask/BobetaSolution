namespace Bobeta.Application.DTOs.Payment;

/// <summary>Request to initiate a MoMo deposit (request-to-pay).</summary>
public record PaymentDepositRequest(string PhoneNumber, decimal Amount);
