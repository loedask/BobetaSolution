using Bobeta.Domain.Entities;

namespace Bobeta.Application.Interfaces;

/// <summary>Persistence contract for wallet entities (one per player).</summary>
public interface IWalletRepository
{
    /// <summary>Gets the wallet for the given player, or null if none exists.</summary>
    Task<Wallet?> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new wallet and returns the same instance.</summary>
    Task<Wallet> AddAsync(Wallet wallet, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing wallet (balance, locked balance, updated at).</summary>
    Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default);
}
