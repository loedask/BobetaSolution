using Bobeta.Client.Contracts;
using Bobeta.Client.Models.Wallet;
using Bobeta.Client.Services.Base;
using BaseApiException = Bobeta.Client.Services.Base.ApiException;

namespace Bobeta.Client.Services;

/// <summary>Client service for wallet operations using the NSwag-generated client.</summary>
public class WalletService(IClient client, HttpClient httpClient, IAccessTokenProvider? accessTokenProvider = null)
    : BaseHttpService(client, httpClient, accessTokenProvider)
{
    public async Task<Response<WalletBalanceViewModel?>> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var getRes = await GetAsync<WalletBalanceDto>("api/Wallet/balance", cancellationToken).ConfigureAwait(false);
            if (!getRes.IsSuccess || getRes.Data == null)
                return Response<WalletBalanceViewModel?>.Failure(getRes.ErrorMessage ?? "Failed to load balance.", getRes.StatusCode);
            var dto = getRes.Data;
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
            var body = new DepositRequest { Amount = amount };
            var postRes = await PostAsync<WalletTransactionDto>("api/Wallet/deposit", body, cancellationToken).ConfigureAwait(false);
            if (!postRes.IsSuccess || postRes.Data == null)
                return Response<WalletTransactionViewModel?>.Failure(postRes.ErrorMessage ?? "Deposit failed.", postRes.StatusCode);
            return Response<WalletTransactionViewModel?>.Success(MapTransaction(postRes.Data));
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
            var body = new WithdrawRequest { Amount = amount };
            var postRes = await PostAsync<WalletTransactionDto>("api/Wallet/withdraw", body, cancellationToken).ConfigureAwait(false);
            if (!postRes.IsSuccess || postRes.Data == null)
                return Response<WalletTransactionViewModel?>.Failure(postRes.ErrorMessage ?? "Withdraw failed.", postRes.StatusCode);
            return Response<WalletTransactionViewModel?>.Success(MapTransaction(postRes.Data));
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
            var uri = $"api/Wallet/transactions?skip={skip}&take={take}";
            var getRes = await GetAsync<List<WalletTransactionDto>>(uri, cancellationToken).ConfigureAwait(false);
            if (!getRes.IsSuccess || getRes.Data == null)
                return Response<IReadOnlyList<WalletTransactionViewModel>>.Failure(getRes.ErrorMessage ?? "Failed to load transactions.", getRes.StatusCode);
            var vms = getRes.Data.Select(MapTransaction).ToList();
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
            Type = MapTransactionType(dto.Type),
            Status = MapTransactionStatus(dto.Status),
            Reference = dto.Reference ?? string.Empty,
            CreatedAt = dto.CreatedAt
        };
    }

    /// <summary>
    /// Map by underlying value so this stays correct after NSwag regen whether members are named <c>_2</c> or <c>BetLock</c>.
    /// </summary>
    private static string MapTransactionType(TransactionType type) => (int)type switch
    {
        0 => "Deposit",
        1 => "Withdrawal",
        2 => "BetLock",
        3 => "BetRelease",
        4 => "Win",
        5 => "Commission",
        _ => type.ToString()
    };

    private static string MapTransactionStatus(TransactionStatus status) => (int)status switch
    {
        0 => "Pending",
        1 => "Completed",
        2 => "Failed",
        3 => "Cancelled",
        _ => status.ToString()
    };
}
