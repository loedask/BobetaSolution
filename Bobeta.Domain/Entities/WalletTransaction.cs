using Bobeta.Domain.Enums;

namespace Bobeta.Domain.Entities;

public class WalletTransaction
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Player Player { get; set; } = null!;
}
