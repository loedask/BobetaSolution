namespace Bobeta.Domain.Enums;

/// <summary>Type of MoMo payment transaction (deposit = request-to-pay, withdrawal = disbursement).</summary>
public enum PaymentTransactionType
{
    Deposit = 0,
    Withdrawal = 1
}
