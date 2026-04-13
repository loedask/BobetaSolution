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
            var dto = await Client.Withdraw2Async(new WithdrawRequest { Amount = amount }, cancellationToken).ConfigureAwait(false);
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
            Type = MapTransactionType(dto.Type),
            Status = MapTransactionStatus(dto.Status),
            Reference = dto.Reference ?? string.Empty,
            CreatedAt = dto.CreatedAt
        };
    }

    /// <summary>
    /// NSwag names enum members <c>_0</c>, <c>_1</c>, … when OpenAPI lacks x-enum-varnames; map to domain names for UI.
    /// </summary>
    private static string MapTransactionType(TransactionType type) => type switch
    {
        TransactionType._0 => "Deposit",
        TransactionType._1 => "Withdrawal",
        TransactionType._2 => "BetLock",
        TransactionType._3 => "BetRelease",
        TransactionType._4 => "Win",
        TransactionType._5 => "Commission",
        _ => type.ToString()
    };

    private static string MapTransactionStatus(TransactionStatus status) => status switch
    {
        TransactionStatus._0 => "Pending",
        TransactionStatus._1 => "Completed",
        TransactionStatus._2 => "Failed",
        TransactionStatus._3 => "Cancelled",
        _ => status.ToString()
    };
}
