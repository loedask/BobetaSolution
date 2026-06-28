using System.Text.Json;
using Bobeta.Client.Json;
using Bobeta.Client.Models.Api;
using Xunit;

namespace Bobeta.Client.Tests.Wallet;

public sealed class WalletTransactionDtoSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = ClientJsonSerializerOptions.Create();

    [Fact]
    public void DeserializeWalletTransactions_ReadsStringEnumTypeAndStatus()
    {
        const string json = """
            [
              {
                "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
                "amount": 5000,
                "type": "Deposit",
                "status": "Completed",
                "reference": "demo-ref",
                "createdAt": "2026-06-28T10:00:00Z"
              }
            ]
            """;

        var items = JsonSerializer.Deserialize<List<WalletTransactionDto>>(json, JsonOptions);

        Assert.NotNull(items);
        Assert.Single(items!);
        Assert.Equal(TransactionType.Deposit, items![0].Type);
        Assert.Equal(TransactionStatus.Completed, items[0].Status);
    }
}
