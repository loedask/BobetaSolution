using Bobeta.Client.Contracts;
using Bobeta.Client.Models.Wallet;
using Bobeta.Client.Services.Base;

namespace Bobeta.Client.Services;

/// <summary>Client service for wallet operations. Placeholder; implement when API calls are required.</summary>
public class WalletService(IClient client, HttpClient httpClient) : BaseHttpService(client, httpClient)
{
    public Task<Response<WalletBalanceViewModel?>> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Response<WalletBalanceViewModel?>.Failure("Not implemented", 501));
    }

    public Task<Response<IReadOnlyList<WalletTransactionViewModel>>> GetTransactionsAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Response<IReadOnlyList<WalletTransactionViewModel>>.Failure("Not implemented", 501));
    }
}
