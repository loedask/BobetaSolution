using Bobeta.Client.Contracts;
using Bobeta.Client.Models.Wallet;
using Bobeta.Client.Services.Base;
using BaseApiException = Bobeta.Client.Services.Base.ApiException;

namespace Bobeta.Client.Services;

/// <summary>Client service for wallet operations using the NSwag-generated client.</summary>
public class WalletService(IClient client, HttpClient httpClient) : BaseHttpService(client, httpClient)
{
    public async Task<Response<WalletBalanceViewModel?>> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = await Client.BalanceAsync(cancellationToken).ConfigureAwait(false);
            var vm = new WalletBalanceViewModel
            {
                Balance = (decimal)dto.Balance,
                LockedBalance = (decimal)dto.LockedBalance
            };
            return Response<WalletBalanceViewModel?>.Success(vm);
        }
        catch (BaseApiException ex)
        {
            return Response<WalletBalanceViewModel?>.Failure(ex.Message, ex.StatusCode);
        }
    }

    public async Task<Response<WalletTransactionViewModel?>> DepositAsync(double amount, CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = await Client.Deposit2Async(new DepositRequest { Amount = amount }, cancellationToken).ConfigureAwait(false);
            return Response<WalletTransactionViewModel?>.Success(MapTransaction(dto));
        }
        catch (BaseApiException ex)
        {
            return Response<WalletTransactionViewModel?>.Failure(ex.Message, ex.StatusCode);
        }
    }

    public async Task<Response<WalletTransactionViewModel?>> WithdrawAsync(double amount, CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = await Client.WithdrawAsync(new WithdrawRequest { Amount = amount }, cancellationToken).ConfigureAwait(false);
            return Response<WalletTransactionViewModel?>.Success(MapTransaction(dto));
        }
        catch (BaseApiException ex)
        {
            return Response<WalletTransactionViewModel?>.Failure(ex.Message, ex.StatusCode);
        }
    }

    public async Task<Response<IReadOnlyList<WalletTransactionViewModel>>> GetTransactionsAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var list = await Client.TransactionsAsync(skip, take, cancellationToken).ConfigureAwait(false);
            var vms = list?.Select(MapTransaction).ToList() ?? new List<WalletTransactionViewModel>();
            return Response<IReadOnlyList<WalletTransactionViewModel>>.Success(vms);
        }
        catch (BaseApiException ex)
        {
            return Response<IReadOnlyList<WalletTransactionViewModel>>.Failure(ex.Message, ex.StatusCode);
        }
    }

    private static WalletTransactionViewModel MapTransaction(WalletTransactionDto dto)
    {
        return new WalletTransactionViewModel
        {
            Id = dto.Id,
            Amount = (decimal)dto.Amount,
            Type = dto.Type.ToString(),
            Status = dto.Status.ToString(),
            Reference = dto.Reference ?? string.Empty,
            CreatedAt = dto.CreatedAt
        };
    }
}
