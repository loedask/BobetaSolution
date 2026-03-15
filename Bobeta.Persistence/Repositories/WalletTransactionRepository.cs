using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Repositories;

/// <summary>Repository implementation for WalletTransaction (append and paged history).</summary>
public class WalletTransactionRepository(BobetaDbContext db) : IWalletTransactionRepository
{
    private readonly BobetaDbContext _db = db;

    public async Task<WalletTransaction> AddAsync(WalletTransaction transaction, CancellationToken cancellationToken = default)
    {
        _db.WalletTransactions.Add(transaction);
        await _db.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    public async Task<IReadOnlyList<WalletTransaction>> GetByPlayerIdAsync(Guid playerId, int skip, int take, CancellationToken cancellationToken = default) =>
        await _db.WalletTransactions
            .Where(t => t.PlayerId == playerId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip).Take(take)
            .ToListAsync(cancellationToken);
}
