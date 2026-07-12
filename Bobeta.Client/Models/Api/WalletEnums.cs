namespace Bobeta.Client.Models.Api;

public enum TransactionType
{
    Deposit = 0,
    Withdrawal = 1,
    BetLock = 2,
    BetRelease = 3,
    Win = 4,
    Commission = 5
}

public enum TransactionStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Cancelled = 3
}
