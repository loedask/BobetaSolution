using Bobeta.Client.Contracts;
using Bobeta.Client.Models.Api;
using Bobeta.Client.Models.Wallet;
using Bobeta.Client.Services.Base;

namespace Bobeta.Client.Services;

/// <summary>Wallet API: balance, deposit, withdraw, transactions.</summary>
public class WalletService(HttpClient httpClient, IAccessTokenProvider? accessTokenProvider = null)
    : BaseHttpService(httpClient, accessTokenProvider)
{
    public async Task<Response<WalletBalanceViewModel?>> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        var getRes = await GetAsync<WalletBalanceDto>("api/Wallet/balance", cancellationToken).ConfigureAwait(false);
        if (!getRes.IsSuccess || getRes.Data == null)
            return Response<WalletBalanceViewModel?>.Failure(getRes.ErrorMessage ?? "Failed to load balance.", getRes.StatusCode);
        var dto = getRes.Data;
        return Response<WalletBalanceViewModel?>.Success(new WalletBalanceViewModel
        {
            Balance = (decimal)dto.Balance,
            LockedBalance = (decimal)dto.LockedBalance
        });
    }

    public async Task<Response<WalletTransactionViewModel?>> DepositAsync(double amount, CancellationToken cancellationToken = default)
    {
        var postRes = await PostAsync<WalletTransactionDto>(
            "api/Wallet/deposit",
            new DepositApiRequest { Amount = amount },
            cancellationToken).ConfigureAwait(false);
        if (!postRes.IsSuccess || postRes.Data == null)
            return Response<WalletTransactionViewModel?>.Failure(postRes.ErrorMessage ?? "Deposit failed.", postRes.StatusCode);
        return Response<WalletTransactionViewModel?>.Success(MapTransaction(postRes.Data));
    }

    public async Task<Response<WalletTransactionViewModel?>> WithdrawAsync(double amount, CancellationToken cancellationToken = default)
    {
        var postRes = await PostAsync<WalletTransactionDto>(
            "api/Wallet/withdraw",
            new WithdrawApiRequest { Amount = amount },
            cancellationToken).ConfigureAwait(false);
        if (!postRes.IsSuccess || postRes.Data == null)
            return Response<WalletTransactionViewModel?>.Failure(postRes.ErrorMessage ?? "Withdraw failed.", postRes.StatusCode);
        return Response<WalletTransactionViewModel?>.Success(MapTransaction(postRes.Data));
    }

    public async Task<Response<IReadOnlyList<WalletTransactionViewModel>>> GetTransactionsAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        var uri = $"api/Wallet/transactions?skip={skip}&take={take}";
        var getRes = await GetAsync<List<WalletTransactionDto>>(uri, cancellationToken).ConfigureAwait(false);
        if (!getRes.IsSuccess || getRes.Data == null)
            return Response<IReadOnlyList<WalletTransactionViewModel>>.Failure(getRes.ErrorMessage ?? "Failed to load transactions.", getRes.StatusCode);
        return Response<IReadOnlyList<WalletTransactionViewModel>>.Success(getRes.Data.Select(MapTransaction).ToList());
    }

    private static WalletTransactionViewModel MapTransaction(WalletTransactionDto dto) => new()
    {
        Id = dto.Id,
        Amount = (decimal)dto.Amount,
        Type = MapTransactionType(dto.Type),
        Status = MapTransactionStatus(dto.Status),
        Reference = dto.Reference ?? string.Empty,
        CreatedAt = dto.CreatedAt
    };

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
