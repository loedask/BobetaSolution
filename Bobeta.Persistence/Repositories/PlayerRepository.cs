using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Repositories;

/// <summary>Repository implementation for Player entities (CRUD and lookup by phone).</summary>
public class PlayerRepository(BobetaDbContext db) : IPlayerRepository
{
    private readonly BobetaDbContext _db = db;

    public async Task<Player?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Players.FindAsync([id], cancellationToken);

    public async Task<Player?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
        await _db.Players.FirstOrDefaultAsync(p => p.PhoneNumber == phoneNumber, cancellationToken);

    public async Task<Player> AddAsync(Player player, CancellationToken cancellationToken = default)
    {
        _db.Players.Add(player);
        await _db.SaveChangesAsync(cancellationToken);
        return player;
    }

    public async Task UpdateAsync(Player player, CancellationToken cancellationToken = default)
    {
        _db.Players.Update(player);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Player> Items, int TotalCount)> GetPagedAsync(
        int skip,
        int take,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Players.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.PhoneNumber.Contains(term) ||
                p.PlayerName.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
