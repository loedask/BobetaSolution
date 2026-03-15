using Bobeta.Domain.Enums;

namespace Bobeta.Domain.Entities;

/// <summary>
/// MTN MoMo payment transaction (request-to-pay for deposit, transfer for withdrawal).
/// Tracks external reference and status until callback or status check confirms result.
/// </summary>
public class PaymentTransaction
{
    /// <summary>Unique identifier for the payment transaction.</summary>
    public Guid Id { get; set; }

    /// <summary>Player who initiated the payment.</summary>
    public Guid PlayerId { get; set; }

    /// <summary>Transaction amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>ISO 4217 currency (e.g. UGX, EUR).</summary>
    public string Currency { get; set; } = "UGX";

    /// <summary>Our external reference used for reconciliation (e.g. sent as externalId to MoMo).</summary>
    public string ExternalReference { get; set; } = string.Empty;

    /// <summary>MoMo transaction/reference id (X-Reference-Id or financialTransactionId from callback).</summary>
    public string? MoMoTransactionId { get; set; }

    /// <summary>Deposit (request-to-pay) or Withdrawal (disbursement).</summary>
    public PaymentTransactionType Type { get; set; }

    /// <summary>Current status: Pending, Success, Failed.</summary>
    public PaymentTransactionStatus Status { get; set; }

    /// <summary>When the transaction was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the transaction was last updated (e.g. callback).</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Navigation to the player.</summary>
    public Player Player { get; set; } = null!;
}
