namespace Bobeta.Application.DTOs.Payment;

/// <summary>Data received from MTN MoMo callback (reference id may come from X-Reference-Id header or body).</summary>
public class MoMoCallbackRequest
{
    /// <summary>Reference id of the transaction (X-Reference-Id or referenceId in body).</summary>
    public string? ReferenceId { get; set; }

    /// <summary>MoMo status: PENDING, SUCCESSFUL, FAILED.</summary>
    public string? Status { get; set; }

    /// <summary>Financial transaction id from MoMo.</summary>
    public string? FinancialTransactionId { get; set; }

    /// <summary>Error code if failed.</summary>
    public string? ReasonCode { get; set; }

    /// <summary>Error message if failed.</summary>
    public string? ReasonMessage { get; set; }
}
