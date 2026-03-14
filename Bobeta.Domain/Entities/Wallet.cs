namespace Bobeta.Domain.Entities;

public class Wallet
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public decimal Balance { get; set; }
    public decimal LockedBalance { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Player Player { get; set; } = null!;
}
