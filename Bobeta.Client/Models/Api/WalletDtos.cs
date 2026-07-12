using System.Text.Json.Serialization;

namespace Bobeta.Client.Models.Api;

public sealed class WalletBalanceDto
{
    [JsonPropertyName("balance")]
    public double Balance { get; set; }

    [JsonPropertyName("lockedBalance")]
    public double LockedBalance { get; set; }
}

public sealed class DepositApiRequest
{
    [JsonPropertyName("amount")]
    public double Amount { get; set; }
}

public sealed class WithdrawApiRequest
{
    [JsonPropertyName("amount")]
    public double Amount { get; set; }
}

public sealed class WalletTransactionDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("type")]
    public TransactionType Type { get; set; }

    [JsonPropertyName("status")]
    public TransactionStatus Status { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}
