namespace Bobeta.Domain.Enums;

/// <summary>Status of a wallet transaction.</summary>
public enum TransactionStatus
{
    /// <summary>Initiated but not yet completed (e.g. pending payment provider).</summary>
    Pending = 0,

    /// <summary>Successfully applied to the wallet.</summary>
    Completed = 1,

    /// <summary>Attempt failed (e.g. insufficient balance, provider error).</summary>
    Failed = 2,

    /// <summary>Transaction was cancelled.</summary>
    Cancelled = 3
}
