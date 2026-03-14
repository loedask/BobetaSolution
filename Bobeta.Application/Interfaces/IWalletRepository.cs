using Bobeta.Domain.Entities;

namespace Bobeta.Application.Interfaces;

public interface IWalletRepository
{
    Task<Wallet?> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task<Wallet> AddAsync(Wallet wallet, CancellationToken cancellationToken = default);
    Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default);
}
