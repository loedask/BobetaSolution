using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Repositories;

/// <summary>Repository implementation for Wallet entities (one per player).</summary>
public class WalletRepository(BobetaDbContext db) : IWalletRepository
{
    private readonly BobetaDbContext _db = db;

    public async Task<Wallet?> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        await _db.Wallets.FirstOrDefaultAsync(w => w.PlayerId == playerId, cancellationToken);

    public async Task<Wallet> AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        _db.Wallets.Add(wallet);
        await _db.SaveChangesAsync(cancellationToken);
        return wallet;
    }

    public async Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        _db.Wallets.Update(wallet);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
