namespace Bobeta.Client.Models.Wallet;

/// <summary>Single wallet transaction view model. Placeholder for API response.</summary>
public class WalletTransactionViewModel
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
