using Bobeta.Domain.Entities;

namespace Bobeta.Application.Interfaces;

/// <summary>Persistence contract for wallet transaction history.</summary>
public interface IWalletTransactionRepository
{
    /// <summary>Appends a new transaction and returns it.</summary>
    Task<WalletTransaction> AddAsync(WalletTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>Returns the most recent transactions for a player, ordered by created date descending, with skip/take for paging.</summary>
    Task<IReadOnlyList<WalletTransaction>> GetByPlayerIdAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default);
}
