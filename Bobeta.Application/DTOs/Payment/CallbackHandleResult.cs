namespace Bobeta.Application.DTOs.Payment;

/// <summary>Result of handling a MoMo callback: whether the transaction was found and whether it was processed (idempotency).</summary>
public enum CallbackHandleResult
{
    /// <summary>No transaction found with the given reference; reject the callback.</summary>
    NotFound,

    /// <summary>Transaction exists but was already processed (not Pending); return success without updating.</summary>
    AlreadyProcessed,

    /// <summary>Transaction was Pending and has been processed (status and wallet updated).</summary>
    Processed
}
