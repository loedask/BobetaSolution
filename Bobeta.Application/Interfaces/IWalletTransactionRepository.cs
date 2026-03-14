using Bobeta.Domain.Entities;

namespace Bobeta.Application.Interfaces;

public interface IWalletTransactionRepository
{
    Task<WalletTransaction> AddAsync(WalletTransaction transaction, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WalletTransaction>> GetByPlayerIdAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default);
}
